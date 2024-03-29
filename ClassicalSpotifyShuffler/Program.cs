﻿using Microsoft.AspNetCore.Components.Web;
using ClassicalSpotifyShuffler;
using BlazorApplicationResources.BlazorApplicationUtils;
using ApplicationResources.Services;
using SpotifyProject.Configuration;
using SpotifyProject.SpotifyUtils;
using SpotifyProject.Authentication;
using System.Net;
using ApplicationResources.Setup;
using CustomResources.Utils.Extensions;
using ClassicalSpotifyShuffler.Implementations;

await BlazorAppUtils.StartApp(
	builder =>
	{
		builder.RootComponents.Add<App>("#app");
		builder.RootComponents.Add<HeadOutlet>("head::after");
		builder.Services.AddBlazorContextMenu();
		builder.Services.AddImplementationForMultipleGlobalServices(new BlazorSpotifyPKCEAuthenticator(), typeof(ISpotifyAccountAuthenticator), typeof(ISpotifyAuthenticator));
		builder.Services.AddSingleton<ISpotifyService>(services => new StandardSpotifyProvider(services.GetSpotifyAuthCodeAuthenticator()));
		return Task.CompletedTask;
	},
	async () =>
	{
		var authorizationSettingsPath = ApplicationResources.Utils.GeneralUtils.GetAbsoluteCombinedPath(Settings.Get<string>(BasicSettings.ProjectRootDirectory), Settings.Get<string>(SpotifySettings.PersonalDataDirectory), GeneralConstants.SuggestedAuthorizationSettingsFile);
		if (await GlobalDependencies.GlobalDependencyContainer.GetLocalDataStore().ExistsAsync(authorizationSettingsPath).WithoutContextCapture())
			await Settings.RegisterProvider(new XmlSettingsProvider(authorizationSettingsPath)).WithoutContextCapture();
		ServicePointManager.DefaultConnectionLimit = Settings.Get<int>(SpotifySettings.NumHTTPConnections);
		await GlobalDependencies.GlobalDependencyContainer.GetSpotifyProvider().InitializeAsync().WithoutContextCapture();
		await GlobalDependencies.GlobalDependencyContainer.GetSpotifyAuthenticator().TryImmediateLogIn().WithoutContextCapture();
	},
	new(args)
	{
		SettingsTypes = new[] { typeof(SpotifySettings) },
		ParameterTypes = new[] { typeof(SpotifyParameters) },
		AdditionalXmlSettingsFiles = new[] { GeneralConstants.StandardConfigurationSettingsFile, GeneralConstants.StandardRunSettingsFile, GeneralConstants.StandardSpotifySettingsFile }
	});