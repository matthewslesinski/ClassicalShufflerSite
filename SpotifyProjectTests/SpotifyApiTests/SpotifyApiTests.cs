﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SpotifyAPI.Web;
using SpotifyProject.SpotifyPlaybackModifier;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackContexts;
using SpotifyProject.SpotifyPlaybackModifier.PlaybackSetters;
using SpotifyProject.SpotifyPlaybackModifier.TrackLinking;
using SpotifyProject.Utils;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Extensions;
using SpotifyProject.Configuration;
using ApplicationResources.ApplicationUtils.Parameters;

namespace SpotifyProjectTests.SpotifyApiTests
{
	public class SpotifyApiTests : SpotifyTestBase
	{

		[Test]
		public async Task CanConnectToSpotify()
		{
			var userProfile = await SpotifyAccessor.GetCurrentUserProfile();
			Assert.IsNotNull(userProfile.DisplayName);
		}

		[Test]
		public async Task RateLimitTest()
		{
			var albumId = SpotifyDependentUtils.TryParseSpotifyUri(SampleAlbumUris[SampleAlbums.BeethovenPianoSonatasAndConcerti], out _, out var parsedId, out _) ? parsedId : null;
			var otherAlbumId = SpotifyDependentUtils.TryParseSpotifyUri(SampleAlbumUris[SampleAlbums.BachKeyboardWorks], out _, out var parsedOtherId, out _) ? parsedOtherId : null;

			var tasks = Enumerable.Range(0, 1000).Select(_ => SpotifyAccessor.GetAlbum(albumId));
			await SpotifyAccessor.GetAlbum(otherAlbumId);
			await Task.WhenAll(tasks);
			Assert.Pass();
		}

		[Test]
		public async Task TestAlbumGetTracks()
		{
			var albumId = SampleAlbumIds[SampleAlbums.BeethovenPianoSonatasAndConcerti];
			var beethovenPianoSonatasTracks = await SpotifyAccessor.GetAllAlbumTracks(albumId, batchSize: 1);
			var beethovenPianoSonatasAlbum = await SpotifyAccessor.GetAlbum(albumId);
			var beethovenPianoSonatasTrackInfos = beethovenPianoSonatasTracks.Select(track => new SimpleTrackAndAlbumWrapper(track, beethovenPianoSonatasAlbum));
			var sortedTracks = beethovenPianoSonatasTrackInfos.OrderBy(ITrackLinkingInfo.TrackOrderWithinAlbums);
			CollectionAssert.AreEqual(beethovenPianoSonatasTrackInfos.Select<ITrackLinkingInfo, string>(track => track.Name), sortedTracks.Select(track => track.Name), "The beethoven sonatas were returned in the wrong order. " +
				$"The expected order was: \n{TurnTracksIntoString(sortedTracks)}\n but the retrieved order was \n {TurnTracksIntoString(beethovenPianoSonatasTrackInfos)}");
		}

		[Test]
		public async Task TestArtistGetTracks()
		{
			using (TaskParameters.GetBuilder().With(SpotifyParameters.ArtistAlbumIncludeGroups, ArtistsAlbumsRequest.IncludeGroups.Album).Apply())
			{
				var artistId = SampleArtistIds[SampleArtists.BelceaQuartet];
				var artistAlbumIncludeGroup = TaskParameters.Get<ArtistsAlbumsRequest.IncludeGroups>(SpotifyParameters.ArtistAlbumIncludeGroups);
				var allArtistTracks = await SpotifyAccessor.GetAllArtistTracks(artistId, artistAlbumIncludeGroup, albumBatchSize: 2).WithoutContextCapture();
				var albumRequest = new ArtistsAlbumsRequest { IncludeGroupsParam = artistAlbumIncludeGroup, Limit = 50, Market = SpotifyAccessor.SpotifyConfiguration.Market };
				var firstAlbumPage = await SpotifyAccessor.Spotify.Artists.GetAlbums(artistId, albumRequest).WithoutContextCapture();
				var allAlbums = await SpotifyAccessor.Spotify.PaginateAll(firstAlbumPage).WithoutContextCapture();
				var totalAlbums = firstAlbumPage.Total ?? -1;
				Assert.AreEqual(totalAlbums, allAlbums.Count);
				var allDistinctAlbums = allAlbums.DistinctOrdered(IAlbumPlaybackContext.SimpleAlbumEqualityComparer);
				var totalExpectedTracksTasks = allDistinctAlbums
					.Select(async album => (await SpotifyAccessor.GetAllAlbumTracks(album.Id).WithoutContextCapture())
					.Where(track => track.Artists.Select(artist => artist.Id).Contains(artistId)).Count()).ToList();
				var totalExpectedTracks = await totalExpectedTracksTasks.MakeAsync().SumAsync().WithoutContextCapture();
				var allFoundAlbumUris = allArtistTracks.Select(track => (track.AlbumUri, track.AlbumName)).DistinctOrdered().ToList();
				var allExpectedAlbumUris = allDistinctAlbums.Select(album => (album.Uri, album.Name)).ToList();
				CollectionAssert.AreEqual(allExpectedAlbumUris, allFoundAlbumUris);
				Assert.AreEqual(totalExpectedTracks, allArtistTracks.Count);
			}
		}

		[Test]
		public async Task TestPlaylistAddingTracks()
		{
			var playlist = await SpotifyAccessor.AddOrGetPlaylistByName(GetPlaylistNameForTest(nameof(TestPlaylistAddingTracks)));
			await SpotifyAccessor.ReplacePlaylistItems(playlist.Id);
			var artistId = SampleArtistIds[SampleArtists.YannickNezetSeguin];
			var yannickTracks = (await SpotifyAccessor.GetAllArtistTracks(artistId, ArtistsAlbumsRequest.IncludeGroups.Album)).ToArray();
			var yannickTrackUris = yannickTracks.Select(track => track.OriginalTrack.Uri);
			var yannickUrisToTracks = yannickTracks.GroupBy(track => track.OriginalTrack.Uri).ToDictionary(group => group.Key, group => group.First());
			var trackBatches = yannickTrackUris.Batch(TaskParameters.Get<int>(SpotifyParameters.PlaylistRequestBatchSize));
			var addTracksTasks = trackBatches.Select(batch => SpotifyAccessor.AddPlaylistItems(playlist.Id, batch)).ToList();
			foreach (var task in addTracksTasks)
				await task;

			var playlistTracks = (await SpotifyAccessor.GetAllRemainingPlaylistTracks(playlist.Id)).ToArray();
			var playlistUrisToTracks = playlistTracks.GroupBy(track => track.Uri).ToDictionary(group => group.Key, group => group.First());

			var playlistUris = playlistTracks.Select(track => track.Uri).ToList();
			var playlistUrisIndices = playlistTracks.Select(track => track.Uri).ToIndexMap();
			Assert.IsTrue(yannickTrackUris.ContainsSameElements(playlistUris, out var diffs),
				"The following tracks were not correctly added to the playlist: " +
				string.Join(", ", diffs.Select(diff => (diff.element, yannickUrisToTracks.TryGetValue(diff.element, out var track)
																		? track.OriginalTrack.Name
																		: playlistUrisToTracks[diff.element].Name, diff.sequence1Count, diff.sequence2Count))
										.Select(diff => $"({diff.element}, {diff.Item2}, {diff.sequence1Count}, {diff.sequence2Count})")));
			foreach(var (batch, batchIndex) in trackBatches.Enumerate())
			{
				var contains = playlistUrisIndices.TryGetValue(batch.First(), out var indices);

				var potentialMatches = indices.Select(firstIndex => playlistUris.GetRange(firstIndex, Math.Min(firstIndex + batch.Count, playlistUris.Count) - firstIndex));

				Assert.That(potentialMatches.Any(match => match.SequenceEqual(batch)),
					$"Batch number {batchIndex + 1} with length {batch.Count} was not added to the playlist in sequential order. It contained tracks: \n" +
					TurnTracksIntoString(batch.Select(uri => yannickUrisToTracks[uri])) +
					$"\n But the playlist match ended up being {(potentialMatches.Count() > 1 ? "one of the following sequences" : "the following sequence")}:\n" +
					string.Join("\n--------------------------------------\n",
						potentialMatches.Select(match => TurnUrisIntoString(match, uri => playlistUrisToTracks[uri].Name,
						uri => (playlistUrisToTracks[uri].DiscNumber, playlistUrisToTracks[uri].TrackNumber),
						uri => playlistUrisToTracks[uri].Album.Name))));
			}
		}

		[Test]
		public async Task TestRemovingTracksFromPlaylist()
		{
			var playlistName = GetPlaylistNameForTest(nameof(TestRemovingTracksFromPlaylist));
			var playlist = await SpotifyAccessor.AddOrGetPlaylistByName(playlistName);
			var playlistContext = new ExistingPlaylistPlaybackContext(SpotifyAccessor.SpotifyConfiguration, playlist);
			var playlistId = playlist.Id;
			var clearTask = SpotifyAccessor.ReplacePlaylistItems(playlistId);
			var testAlbum = SampleAlbums.BachCantatas;
			var albumId = SampleAlbumIds[testAlbum];
			var album = new ExistingAlbumPlaybackContext(SpotifyAccessor.SpotifyConfiguration, await SpotifyAccessor.GetAlbum(albumId));
			await album.FullyLoad();
			var albumTracks = album.PlaybackOrder.Select(track => album.As<ISpotifyPlaybackContext<SimpleTrack>>().GetMetadataForTrack(track));
			CollectionAssert.IsNotEmpty(albumTracks.Where(track => track.IsPlayable && track.TryGetLinkedTrack(out _)));
			var originalAlbumTracks = albumTracks.Select(track => track.GetOriginallyRequestedVersion());
			var albumUris = originalAlbumTracks.Select(track => track.Uri);
			var albumUrisToAdd = new string[] { albumUris.First(), albumUris.First() };
			await clearTask;
			await SpotifyAccessor.AddPlaylistItems(playlistId, albumUrisToAdd);
			await SpotifyAccessor.RemovePlaylistItems(playlistId, null, albumTracks.Select(track => track.Uri).Take(1));
			await playlistContext.FullyLoad();
			var playlistTracks = playlistContext.PlaybackOrder.Select(playlistContext.GetMetadataForTrack).Select(track => track.GetOriginallyRequestedVersion());
			CollectionAssert.AreEquivalent(albumUrisToAdd, playlistTracks.Select(track => track.Uri));
			await SpotifyAccessor.RemovePlaylistItems(playlistId, null, albumUris.Take(1));
			await playlistContext.FullyLoad();
			playlistTracks = playlistContext.PlaybackOrder.Select(playlistContext.GetMetadataForTrack).Select(track => track.GetOriginallyRequestedVersion());
			CollectionAssert.IsEmpty(playlistTracks);
		}

		[TestCase("BrahmsSymphonies", 0, "reversed order by album index")]
		[TestCase("BrahmsSymphonies", 1, "order by album index")]
		public async Task TestReorderingPlaylist(string albumToUse, int testCaseIndex, string testCaseDescriptor)
		{
			IComparer<ITrackLinkingInfo>[] TestCasesForTestReorderingPlaylist = new [] {
				ITrackLinkingInfo.TrackOrderWithinAlbums.Reversed(),
				ITrackLinkingInfo.TrackOrderWithinAlbums,
			};

			var albumEnum = Enum.Parse<SampleAlbums>(albumToUse, true);
			var testCaseOrdering = TestCasesForTestReorderingPlaylist[testCaseIndex];
			var playlist = await SpotifyAccessor.AddOrGetPlaylistByName(GetPlaylistNameForTest(nameof(TestReorderingPlaylist)));
			await SpotifyAccessor.ReplacePlaylistItems(playlist.Id);
			var albumId = SpotifyDependentUtils.TryParseSpotifyUri(SampleAlbumUris[albumEnum], out _, out var parsedId, out _) ? parsedId : null;
			var tracks = await SpotifyAccessor.GetAllAlbumTracks(albumId);
			var album = await SpotifyAccessor.GetAlbum(albumId);
			var trackInfos = tracks.Select<SimpleTrack, IPlayableTrackLinkingInfo>(track => new SimpleTrackAndAlbumWrapper(track, album));
			var sortedTrackInfos = trackInfos.OrderBy(testCaseOrdering);
			await SpotifyAccessor.AddPlaylistItems(playlist.Id, sortedTrackInfos.Select(track => track.Uri));
			var existingPlaylistContext = new ExistingPlaylistPlaybackContext(SpotifyAccessor.SpotifyConfiguration, playlist);
			await existingPlaylistContext.FullyLoad();
			CollectionAssert.AreEqual(sortedTrackInfos.Select(track => track.Uri), existingPlaylistContext.PlaybackOrder.Select(track => track.Uri));
			IPlaylistTrackModifier playlistModifier = new EfficientPlaylistTrackModifier(SpotifyAccessor.SpotifyConfiguration);
			await playlistModifier.ModifyPlaylistTracks(existingPlaylistContext, trackInfos);
			var newTracks = await SpotifyAccessor.GetAllRemainingPlaylistTracks(playlist.Id);
			CollectionAssert.AreEqual(trackInfos.Select(track => track.Uri), newTracks.Select(track => track.Uri), "The playlist resulted in the wrong order. " +
				$"The expected order was: \n{TurnTracksIntoString(trackInfos)}\n but the retrieved order was \n {TurnTracksIntoString(newTracks.Select(existingPlaylistContext.GetMetadataForTrack))}");
		}
	}
}
