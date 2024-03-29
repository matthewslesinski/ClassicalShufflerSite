﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using CustomResources.Utils.Extensions;
using ApplicationResources.Logging;
using System.Threading;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public abstract class AlbumPlaybackContext : SpotifyPlaybackQueueBase<SimpleTrack>, IAlbumPlaybackContext
	{
		public AlbumPlaybackContext(SpotifyConfiguration spotifyConfiguration, FullAlbum album) : base(spotifyConfiguration)
		{
			SpotifyContext = album;
		}

		public FullAlbum SpotifyContext { get; }
	}

	public class ExistingAlbumPlaybackContext : AlbumPlaybackContext, IOriginalAlbumPlaybackContext
	{
		public ExistingAlbumPlaybackContext(SpotifyConfiguration spotifyConfiguration, FullAlbum album) : base(spotifyConfiguration, album)
		{
		}

		public static async Task<ExistingAlbumPlaybackContext> FromSimpleAlbum(SpotifyConfiguration spotifyConfiguration, string albumId, CancellationToken cancellationToken = default)
		{
			var fullAlbum = await spotifyConfiguration.GetAlbum(albumId, cancellationToken).WithoutContextCapture();
			return new ExistingAlbumPlaybackContext(spotifyConfiguration, fullAlbum);
		}

		public async Task FullyLoad(CancellationToken cancellationToken = default)
		{
			Logger.Information($"Requesting all tracks for album with id {SpotifyContext.Id} and name {SpotifyContext.Name}");
			var allTracks = await this.GetAllAlbumTracks(SpotifyContext.Id, cancellationToken: cancellationToken).WithoutContextCapture();
			Logger.Information($"Loaded {allTracks.Count} tracks");
			PlaybackOrder = allTracks;
		}

	}

	public class ReorderedAlbumPlaybackContext<OriginalContextT> : AlbumPlaybackContext, IReorderedPlaybackContext<SimpleTrack, OriginalContextT>
		where OriginalContextT : IAlbumPlaybackContext
	{
		public ReorderedAlbumPlaybackContext(OriginalContextT baseContext, IEnumerable<SimpleTrack> reorderedTracks) : base(baseContext.SpotifyConfiguration, baseContext.SpotifyContext)
		{
			PlaybackOrder = reorderedTracks;
			BaseContext = baseContext;
		}

		public OriginalContextT BaseContext { get; }

		public static ReorderedAlbumPlaybackContext<OriginalContextT> FromContextAndTracks(OriginalContextT originalContext, IEnumerable<SimpleTrack> tracks) =>
			new ReorderedAlbumPlaybackContext<OriginalContextT>(originalContext, tracks);
	}
}
