﻿using System;
using System.Collections.Generic;
using ApplicationResources.Utils;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using CustomResources.Utils.Extensions;

namespace SpotifyProject.SpotifyPlaybackModifier.Transformations
{
	public class SimpleReordering<InputContextT, OutputContextT, TrackT> : ITrackReorderingPlaybackTransformation<InputContextT, OutputContextT, TrackT>
		where InputContextT : ISpotifyPlaybackContext<TrackT> where OutputContextT : IReorderedPlaybackContext<TrackT, InputContextT>
	{
		private readonly Func<InputContextT, IEnumerable<TrackT>, OutputContextT> _contextConstructor;

		public SimpleReordering(Func<InputContextT, IEnumerable<TrackT>, OutputContextT> contextConstructor)
		{
			_contextConstructor = contextConstructor;
		}

		OutputContextT ITrackReorderingPlaybackTransformation<InputContextT, OutputContextT, TrackT>.ConstructNewContext(InputContextT inputContext, IEnumerable<TrackT> newTrackOrder)
		{
			return _contextConstructor(inputContext, newTrackOrder);
		}

		IEnumerable<TrackT> ITrackReorderingPlaybackTransformation<InputContextT, OutputContextT, TrackT>.Reorder(InputContextT originalContext, IEnumerable<TrackT> tracks)
		{
			return tracks.RandomShuffle();
		}
	}
}
