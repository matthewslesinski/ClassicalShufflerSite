﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using CustomResources.Utils.Extensions;
using ApplicationResources.Logging;
using System.Threading;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public abstract class ArtistPlaybackContext : SpotifyPlaybackQueueBase<SimpleTrackAndAlbumWrapper>, IArtistPlaybackContext
	{
		public ArtistPlaybackContext(SpotifyConfiguration spotifyConfiguration, FullArtist artist) : base(spotifyConfiguration)
		{
			SpotifyContext = artist;
		}

		public FullArtist SpotifyContext { get; }
	}

	public class ExistingArtistPlaybackContext : ArtistPlaybackContext, IOriginalArtistPlaybackContext
	{
		public ExistingArtistPlaybackContext(SpotifyConfiguration spotifyConfiguration, FullArtist artist, ArtistsAlbumsRequest.IncludeGroups albumTypesToInclude) : base(spotifyConfiguration, artist)
		{
			_albumGroupsToInclude = albumTypesToInclude;
		}

		public static async Task<ExistingArtistPlaybackContext> FromSimpleArtist(SpotifyConfiguration spotifyConfiguration, string artistId, ArtistsAlbumsRequest.IncludeGroups albumTypesToInclude, CancellationToken cancellationToken = default)
		{
			var fullArtist = await spotifyConfiguration.GetArtist(artistId, cancellationToken).WithoutContextCapture();
			return new ExistingArtistPlaybackContext(spotifyConfiguration, fullArtist, albumTypesToInclude);
		}

		private readonly ArtistsAlbumsRequest.IncludeGroups _albumGroupsToInclude;

		public async Task FullyLoad(CancellationToken cancellationToken = default)
		{
			Logger.Information($"Loading albums for artist with Id {SpotifyContext.Id} and Name {SpotifyContext.Name}");
			var allTracks = await this.GetAllArtistTracks(SpotifyContext.Id, _albumGroupsToInclude, cancellationToken: cancellationToken).WithoutContextCapture();
			Logger.Information($"All {allTracks.Count} tracks loaded");
			PlaybackOrder = allTracks;
		}
	}

	public class ReorderedArtistPlaybackContext<OriginalContextT> : ArtistPlaybackContext, IReorderedPlaybackContext<SimpleTrackAndAlbumWrapper, OriginalContextT>
		where OriginalContextT : IArtistPlaybackContext
	{
		public ReorderedArtistPlaybackContext(OriginalContextT baseContext, IEnumerable<SimpleTrackAndAlbumWrapper> reorderedTracks) : base(baseContext.SpotifyConfiguration, baseContext.SpotifyContext)
		{
			PlaybackOrder = reorderedTracks;
			BaseContext = baseContext;
		}

		public OriginalContextT BaseContext { get; }

		public static ReorderedArtistPlaybackContext<OriginalContextT> FromContextAndTracks(OriginalContextT originalContext, IEnumerable<SimpleTrackAndAlbumWrapper> tracks) =>
			new ReorderedArtistPlaybackContext<OriginalContextT>(originalContext, tracks);
	}
}
