﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ApplicationResources.Services;
using CustomResources.Utils.Extensions;
using Util = CustomResources.Utils.GeneralUtils.Utils;

namespace ApplicationResources.Setup
{
	public class XmlSettingsProvider : SettingsParserBase, IGlobalServiceUser
	{
		private readonly string _fileName;
		private readonly IEnumerable<Enum> _requiredSettings;
		private Dictionary<Enum, IEnumerable<string>> _loadedValues;
		public XmlSettingsProvider(string fileName, params Enum[] requiredSettings)
		{
			_fileName = fileName;
			_requiredSettings = requiredSettings;
		}

		public override IEnumerable<Enum> LoadedSettings => _loadedValues.Keys;

		public override async Task<IEnumerable<Enum>> Load(CancellationToken cancellationToken = default)
		{
			await Util.LoadOnceBlockingAsync(_isLoaded, _lock, async (cancellationToken) =>
			{
				var fileContents = await this.AccessLocalDataStore().GetAsync(_fileName, CachePolicy.PreferActual, cancellationToken).WithoutContextCapture();
				var doc = XElement.Load(new StringReader(fileContents));
				_loadedValues = doc.Descendants(_settingNodeName)
					.Select(node => (node.Attribute(_settingNodeIdentifier).Value, node.Value))
					.GroupBy(GeneralExtensions.GetFirst, GeneralExtensions.GetSecond)
					.ToDictionary<IGrouping<string, string>, Enum, IEnumerable<string>>(group => AllSettings[group.Key], group => group.ToList());
				var missingSettings = _requiredSettings.Where(_loadedValues.NotContainsKey);
				if (missingSettings.Any())
					throw new KeyNotFoundException($"In order to use {_fileName} for settings, it must specify a value for the following settings: {string.Join(", ", missingSettings)}");
			}, cancellationToken).WithoutContextCapture();
			return LoadedSettings;
		}

		public override string ToString() => $"{nameof(XmlSettingsProvider)}({_fileName})";

		protected override bool TryGetValues(Enum setting, out IEnumerable<string> values) => _loadedValues.TryGetValue(setting, out values);

		private const string _settingNodeName = "Setting";
		private const string _settingNodeIdentifier = "name";
	}
}
