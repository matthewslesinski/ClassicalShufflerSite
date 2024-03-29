﻿using System;
using System.Linq;
using System.Collections.Generic;
using SpotifyAPI.Web;
using System.Threading.Tasks;
using CustomResources.Utils.Extensions;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using ApplicationResources.Logging;
using System.Threading;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public abstract class PlaylistPlaybackContext : SpotifyPlaybackQueueBase<FullTrack>, IPlaylistPlaybackContext<FullTrack>
	{
		public PlaylistPlaybackContext(SpotifyConfiguration spotifyConfiguration, FullPlaylist playlist) : base(spotifyConfiguration)
		{
			SpotifyContext = playlist;
		}

		public IPlayableTrackLinkingInfo<FullTrack> GetMetadataForTrack(FullTrack track)
		{
			return new FullTrackWrapper(track);
		}

		public FullPlaylist SpotifyContext { get; }
	}

	public class ExistingPlaylistPlaybackContext : PlaylistPlaybackContext, IOriginalPlaylistPlaybackContext
	{
		public ExistingPlaylistPlaybackContext(SpotifyConfiguration spotifyConfiguration, FullPlaylist playlist) : base(spotifyConfiguration, playlist)
		{
		}

		public static async Task<ExistingPlaylistPlaybackContext> FromSimplePlaylist(SpotifyConfiguration spotifyConfiguration, string playlistId, CancellationToken cancellationToken = default)
		{
			var fullPlaylist = await spotifyConfiguration.Spotify.Playlists.Get(playlistId).WaitAsync(cancellationToken).WithoutContextCapture();
			return new ExistingPlaylistPlaybackContext(spotifyConfiguration, fullPlaylist);
		}

		public async Task FullyLoad(CancellationToken cancellationToken = default)
		{
			Logger.Information($"Loading tracks for playlist with Id {SpotifyContext.Id} and Name {SpotifyContext.Name}");
			var allTracks = await this.GetAllRemainingPlaylistTracks(SpotifyContext.Id, cancellationToken: cancellationToken).WithoutContextCapture();
			Logger.Information($"Loaded {allTracks.Count} tracks");
			PlaybackOrder = allTracks;
		}
	}

	public class ReorderedPlaylistPlaybackContext<OriginalContextT> : PlaylistPlaybackContext, IReorderedPlaybackContext<FullTrack, OriginalContextT>
		where OriginalContextT : IPlaylistPlaybackContext<FullTrack>
	{
		public ReorderedPlaylistPlaybackContext(OriginalContextT baseContext, IEnumerable<FullTrack> reorderedTracks) : base(baseContext.SpotifyConfiguration, baseContext.SpotifyContext)
		{
			PlaybackOrder = reorderedTracks;
			BaseContext = baseContext;
		}

		public OriginalContextT BaseContext { get; }

		public static ReorderedPlaylistPlaybackContext<OriginalContextT> FromContextAndTracks(OriginalContextT originalContext, IEnumerable<FullTrack> tracks) =>
			new ReorderedPlaylistPlaybackContext<OriginalContextT>(originalContext, tracks);
	}

	public class ConstructedPlaylistContext<TrackT, OriginalContextT> : SpotifyPlaybackQueueBase<TrackT>, IPlaylistPlaybackContext<TrackT>, IReorderedPlaybackContext<TrackT, OriginalContextT>
		where OriginalContextT : ISpotifyPlaybackContext<TrackT>
	{
		public ConstructedPlaylistContext(OriginalContextT originalContext, IPlaylistPlaybackContext<FullTrack> originalPlaylistVersion) : base(originalContext.SpotifyConfiguration)
		{
			PlaybackOrder = originalContext.PlaybackOrder;
			BaseContext = originalContext;
			SpotifyContext = originalPlaylistVersion.SpotifyContext;
		}

		public OriginalContextT BaseContext { get; }
		public IPlayableTrackLinkingInfo<TrackT> GetMetadataForTrack(TrackT track)
		{
			return BaseContext.GetMetadataForTrack(track);
		}

		public FullPlaylist SpotifyContext { get; }
	}
}
