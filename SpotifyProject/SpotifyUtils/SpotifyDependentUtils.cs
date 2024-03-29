﻿using System;
using SpotifyAPI.Web;

namespace SpotifyProject.Utils
{
	/** Utility methods that require imports, for instance the Spotify API */
	public static class SpotifyDependentUtils
	{
		public static bool TryParseUriFromLink(string contextLink, out string contextUri)
		{
			contextUri = default;
			if (!TryParseSpotifyContextLink(contextLink, out var typeString, out var contextId))
				return false;
			contextUri = $"{SpotifyConstants.SpotifyUriPrefix}{typeString}:{contextId}";
			return true;
		}

		public static bool TryParseSpotifyContextLink(string contextLink, out string type, out string id)
		{
			type = null;
			id = null;
			if (!contextLink.StartsWith(SpotifyConstants.OpenSpotifyUrl))
				return false;
			var allParts = contextLink.Split('/', StringSplitOptions.RemoveEmptyEntries);
			type = allParts[^2];
			var idPart = allParts[^1];
			var questionIndex = idPart.IndexOf('?');
			id = questionIndex >= 0 ? idPart.Substring(0, questionIndex) : idPart;
			return true;
		}

		public static bool TryParseSpotifyUri(string uri, out string type, out string id, out string[] allParts)
		{
			allParts = uri.Split(SpotifyConstants.UriPartDivider, StringSplitOptions.RemoveEmptyEntries);
			if (allParts.Length < 3)
			{
				type = null;
				id = null;
				allParts = null;
				return false;
			}
			type = allParts[^2];
			id = allParts[^1];
			return true;
		}
	}
}
