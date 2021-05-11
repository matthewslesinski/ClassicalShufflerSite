﻿using System;
using System.Linq;
using System.Collections.Generic;
using SpotifyAPI.Web;
using System.Threading.Tasks;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;

namespace SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts
{
	public abstract class AllLikedTracksPlaybackContext : SpotifyPlaybackQueueBase<FullTrack>, IAllLikedTracksPlaybackContext
	{
		public AllLikedTracksPlaybackContext(SpotifyConfiguration spotifyConfiguration) : base(spotifyConfiguration)
		{
		}

		public ITrackLinkingInfo GetMetadataForTrack(FullTrack track)
		{
			return new FullTrackWrapper(track);
		}
	}

	public class ExistingAllLikedTracksPlaybackContext : AllLikedTracksPlaybackContext, IOriginalAllLikedTracksPlaybackContext
	{
		public ExistingAllLikedTracksPlaybackContext(SpotifyConfiguration spotifyConfiguration) : base(spotifyConfiguration)
		{
		}

		public async Task FullyLoad()
		{
			Logger.Information($"Requesting all saved tracks from Spotify");
			var allItems = Spotify.Paginate(await Spotify.Library.GetTracks(new LibraryTracksRequest { Limit = 50, Market = _relevantMarket }));
			var allTracks = await allItems.Select(track => track.Track).OfType<FullTrack>().ToListAsync();
			Logger.Information($"Loaded {allTracks.Count()} tracks");
			PlaybackOrder = allTracks;
		}
	}

	public class ReorderedAllLikedTracksPlaybackContext<OriginalContextT> : AllLikedTracksPlaybackContext, IReorderedPlaybackContext<FullTrack, OriginalContextT>
		where OriginalContextT : IAllLikedTracksPlaybackContext
	{
		public ReorderedAllLikedTracksPlaybackContext(OriginalContextT baseContext, IEnumerable<FullTrack> reorderedTracks) : base(baseContext.SpotifyConfiguration)
		{
			PlaybackOrder = reorderedTracks;
			BaseContext = baseContext;
		}

		public OriginalContextT BaseContext { get; }

		public static ReorderedAllLikedTracksPlaybackContext<OriginalContextT> FromContextAndTracks(OriginalContextT originalContext, IEnumerable<FullTrack> tracks) =>
			new ReorderedAllLikedTracksPlaybackContext<OriginalContextT>(originalContext, tracks);
	}
}