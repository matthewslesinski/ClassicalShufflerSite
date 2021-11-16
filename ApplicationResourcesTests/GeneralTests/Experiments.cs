﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CustomResources.Utils.Concepts.DataStructures;
using NUnit.Framework;

namespace ApplicationResourcesTests.GeneralTests
{
	public class Experiments : GeneralTestBase
	{
		[Test]
		public void TestInternalConcurrentDictionaryOverrides()
		{
			var dict = new InternalConcurrentDictionary<int, int>();
			ConcurrentDictionary<int, int> asConcurrent = dict;
			IDictionary<int, int> asIDictionary = asConcurrent;
			IReadOnlyDictionary<int, int> asReadOnly = asConcurrent;

			Assert.AreEqual(typeof(KeyCollectionView<int, int, InternalConcurrentDictionary<int, int>>), dict.Keys.GetType());
			Assert.AreEqual(typeof(ReadOnlyCollection<int>), asConcurrent.Keys.GetType());
			Assert.AreEqual(typeof(KeyCollectionView<int, int, InternalConcurrentDictionary<int, int>>), asIDictionary.Keys.GetType());
			Assert.AreEqual(typeof(KeyCollectionView<int, int, InternalConcurrentDictionary<int, int>>), asReadOnly.Keys.GetType());
		}

		[Test]
		public async Task TestConfigureAwaitWithAsyncLocal()
		{
			var singleValue = 0;
			var checks = 0;
			var asyncLocal = new AsyncLocal<int> { Value = singleValue };
			Assert.AreEqual(singleValue, asyncLocal.Value);
			async Task RunDelayedTest(int order, int delay, int expectedChecks)
			{
				Assert.Less(singleValue, order);
				singleValue = order;
				asyncLocal.Value = order;
				await Task.Delay(delay).ConfigureAwait(false);
				Assert.AreEqual(expectedChecks, checks++);
				Assert.AreEqual(order, asyncLocal.Value);
			}
			var task1 = RunDelayedTest(1, 100, 1).ConfigureAwait(false);
			var task2 = RunDelayedTest(2, 50, 0).ConfigureAwait(false);
			await task1;
			await task2;
			Assert.AreEqual(2, checks);
			Assert.AreEqual(0, asyncLocal.Value);
		}

		[Test]
		public Task TestAsyncLocalUniqueToThreads()
		{
			var asyncVal = new AsyncLocal<int> { Value = -1 };
			Task RunTest(int uniqueValue)
			{
				return Task.Run(() =>
				{
					asyncVal.Value = uniqueValue;
					Task.WaitAll(Task.Delay(100).ContinueWith(completedTask => Assert.AreEqual(uniqueValue, asyncVal.Value)));
				});
					
			}
			return Task.WhenAll(RunTest(1), RunTest(2));
		}
	}
}