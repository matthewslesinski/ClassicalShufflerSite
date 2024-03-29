﻿using System;
using ApplicationResources.Services;
using SpotifyAPI.Web;
using SpotifyProject.Utils;

namespace SpotifyProject.SpotifyPlaybackModifier
{
	public class SpotifyAccessorBase : ISpotifyAccessor, IGlobalServiceUser
	{
		public SpotifyAccessorBase(SpotifyConfiguration spotifyConfiguration)
		{
			SpotifyConfiguration = spotifyConfiguration;
		}

		public SpotifyAccessorBase(SpotifyClient spotifyClient) : this(new SpotifyConfiguration { Spotify = spotifyClient, Market = SpotifyConstants.USMarket })
		{ }

		public SpotifyConfiguration SpotifyConfiguration { get; }
		public SpotifyClient Spotify => SpotifyConfiguration.Spotify;

	}
}
