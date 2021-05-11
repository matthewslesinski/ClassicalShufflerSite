﻿using System;
using SpotifyAPI.Web;

namespace SpotifyProject.Utils
{
	/** Utility methods that require imports, for instance the Spotify API */
	public static class SpotifyDependentUtils
	{
		public static bool IsIdenticalTo(this AuthorizationCodeTokenResponse tokenResponse, AuthorizationCodeTokenResponse otherResponse)
		{
			return Equals(tokenResponse, otherResponse) || (Equals(tokenResponse.AccessToken, otherResponse.AccessToken)
				&& Equals(tokenResponse.RefreshToken, otherResponse.RefreshToken)
				&& Equals(tokenResponse.CreatedAt, otherResponse.CreatedAt)
				&& Equals(tokenResponse.ExpiresIn, otherResponse.ExpiresIn)
				&& Equals(tokenResponse.TokenType, otherResponse.TokenType)
				&& Equals(tokenResponse.Scope, otherResponse.Scope));
		}

		public static int Hash(this AuthorizationCodeTokenResponse tokenResponse)
		{
			return (tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.CreatedAt, tokenResponse.ExpiresIn, tokenResponse.TokenType, tokenResponse.Scope).GetHashCode();
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
			type = allParts[allParts.Length - 2];
			id = allParts[allParts.Length - 1];
			return true;
		}
	}
}