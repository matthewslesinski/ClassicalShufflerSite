﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomResources.Utils.Extensions;
using CustomResources.Utils.GeneralUtils;

namespace CustomResources.Utils.Concepts.DataStructures
{
	public interface IInternalCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
	{
		void ICollection.CopyTo(Array array, int index)
		{
			Ensure.ArgumentNotNull(array, nameof(array));
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			var space = array.Length - index;
			if (this.As<ICollection<T>>().Count > space)
				throw new ArgumentException("Not enough space in destination array to fit the elements");
			foreach (var element in this)
				array.SetValue(element, index++);
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex) => CopyTo(array.As<Array>(), arrayIndex);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	public interface IElementContainer<T>
	{
		IEqualityComparer<T> EqualityComparer { get; }
	}

	public interface IMappedElementContainer<S, T> : IElementContainer<S>
	{
		IEqualityComparer<T> WrappedEqualityComparer { get; }
		Func<S, T> ElementMapper { get; }
		IEqualityComparer<S> IElementContainer<S>.EqualityComparer => new KeyBasedEqualityComparer<S, T>(ElementMapper, WrappedEqualityComparer);
	}

	#region Set Interfaces

	public interface IInternalSet<T> : ISet<T>, IReadOnlySet<T>, IInternalCollection<T>, IElementContainer<T>
	{
		void ICollection<T>.Add(T item)
		{
			if (IsReadOnly)
				throw new NotSupportedException("Cannot add elements to a readonly set");
			Add(item);
		}

		void ISet<T>.ExceptWith(IEnumerable<T> other) => ExceptWithNaive(other);
		protected sealed void ExceptWithNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			foreach (var o in other)
				Remove(o);
		}

		void ISet<T>.IntersectWith(IEnumerable<T> other) => IntersectWithNaive(other);
		protected sealed void IntersectWithNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			if (other is not ISet<T> otherSet)
				otherSet = other.ToHashSet();
			var toRemoves = this.Where(otherSet.NotContains).ToList();
			foreach (var toRemove in toRemoves)
				Remove(toRemove);
		}

		bool IReadOnlySet<T>.IsProperSubsetOf(IEnumerable<T> other) => this.As<ISet<T>>().IsProperSubsetOf(other);

		bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other) =>
			(other is ISet<T> otherSet ? otherSet.Count : other.EnsureNotNull(nameof(other)).Distinct(EqualityComparer).Count()) > this.As<ISet<T>>().Count
				&& this.As<ISet<T>>().IsSubsetOf(other);

		bool IReadOnlySet<T>.IsProperSupersetOf(IEnumerable<T> other) => this.As<ISet<T>>().IsProperSupersetOf(other);

		bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other) =>
			(other is ISet<T> otherSet ? otherSet.Count : other.EnsureNotNull(nameof(other)).Distinct(EqualityComparer).Count()) < this.As<ISet<T>>().Count
				&& this.As<ISet<T>>().IsSupersetOf(other);

		bool IReadOnlySet<T>.IsSubsetOf(IEnumerable<T> other) => this.As<ISet<T>>().IsSubsetOf(other);

		bool ISet<T>.IsSubsetOf(IEnumerable<T> other) => IsSubsetOfNaive(other);
		protected sealed bool IsSubsetOfNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			var count = this.As<ISet<T>>().Count;
			return !(other is ISet<T> otherSet && otherSet.Count < count) && other.Where(this.As<ISet<T>>().Contains).Distinct(EqualityComparer).Count() == count;
		}

		bool IReadOnlySet<T>.IsSupersetOf(IEnumerable<T> other) => this.As<ISet<T>>().IsSupersetOf(other);

		bool ISet<T>.IsSupersetOf(IEnumerable<T> other) => IsSupersetOfNaive(other);
		protected sealed bool IsSupersetOfNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			var count = this.As<ICollection<T>>().Count;
			return !(other is ISet<T> otherSet && otherSet.Count > count) && other.All(this.As<ISet<T>>().Contains);
		}

		bool IReadOnlySet<T>.Overlaps(IEnumerable<T> other) => this.As<ISet<T>>().Overlaps(other);

		bool ISet<T>.Overlaps(IEnumerable<T> other) => OverlapsNaive(other);
		protected sealed bool OverlapsNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			return other.Any(this.As<ISet<T>>().Contains);
		}

		bool IReadOnlySet<T>.SetEquals(IEnumerable<T> other) => this.As<ISet<T>>().SetEquals(other);

		bool ISet<T>.SetEquals(IEnumerable<T> other) => SetEqualsNaive(other);
		protected sealed bool SetEqualsNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			if (other is not ISet<T> otherSet)
				otherSet = other.ToHashSet(EqualityComparer);
			var count = this.As<ISet<T>>().Count;
			return otherSet.Count == count && this.Intersect(otherSet).Count() == count;
		}

		void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) => SymmetricExceptWithNaive(other);
		protected sealed void SymmetricExceptWithNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			foreach (var o in other.Distinct(EqualityComparer))
			{
				if (this.As<ISet<T>>().Contains(o))
					Remove(o);
				else
					Add(o);
			}
		}

		void ISet<T>.UnionWith(IEnumerable<T> other) => UnionWithNaive(other);
		protected sealed void UnionWithNaive(IEnumerable<T> other)
		{
			Ensure.ArgumentNotNull(other, nameof(other));
			foreach (var o in other)
				Add(o);
		}
	}


	public interface IInternalSet<T, in SetT> : IInternalSet<T> where SetT : IInternalSet<T, SetT>, IReadOnlySet<T>
	{
		void ISet<T>.ExceptWith(IEnumerable<T> other)
		{
			if (other is SetT otherSet && ExceptWith(otherSet))
				return;
			ExceptWithNaive(other);			
		}

		bool ExceptWith(SetT otherSet);

		void ISet<T>.IntersectWith(IEnumerable<T> other)
		{
			if (other is SetT otherSet && IntersectWith(otherSet))
				return;
			IntersectWithNaive(other);
		}

		bool IntersectWith(SetT otherSet);

		bool ISet<T>.IsSubsetOf(IEnumerable<T> other) => other is SetT otherSetT ? IsSubsetOf(otherSetT) : IsSubsetOfNaive(other);

		bool IsSubsetOf(SetT otherSet);

		bool ISet<T>.IsSupersetOf(IEnumerable<T> other) => other is SetT otherSetT ? IsSupersetOf(otherSetT) : IsSupersetOfNaive(other);

		bool IsSupersetOf(SetT otherSet);

		bool ISet<T>.Overlaps(IEnumerable<T> other) => other is SetT otherSetT ? Overlaps(otherSetT) : OverlapsNaive(other);

		bool Overlaps(SetT otherSet);

		bool ISet<T>.SetEquals(IEnumerable<T> other) => other is SetT otherSetT ? SetEquals(otherSetT) : SetEqualsNaive(other);

		bool SetEquals(SetT otherSet);

		void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
		{
			if (other is SetT otherSet && SymmetricExceptWith(otherSet))
				return;
			SymmetricExceptWithNaive(other);
		}

		bool SymmetricExceptWith(SetT otherSet);

		void ISet<T>.UnionWith(IEnumerable<T> other)
		{
			if (other is SetT otherSet && UnionWith(otherSet))
				return;
			UnionWithNaive(other);
		}

		bool UnionWith(SetT otherSet);
	}

	#endregion

	public interface IInternalDictionary<K, V> : IDictionary<K, V>, IReadOnlyDictionary<K, V>, IInternalCollection<KeyValuePair<K, V>>, IElementContainer<K>
	{
		IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => Keys;
		ICollection<K> IDictionary<K, V>.Keys => Keys;
		public new KeyCollectionView<K, V, IInternalDictionary<K, V>> Keys => new KeyCollectionView<K, V, IInternalDictionary<K, V>>(this, EqualityComparer);

		IEnumerable<V> IReadOnlyDictionary<K, V>.Values => Values;
		ICollection<V> IDictionary<K, V>.Values => Values;
		public new ValueCollectionView<K, V, IInternalDictionary<K, V>> Values => new ValueCollectionView<K, V, IInternalDictionary<K, V>>(this);

		void ICollection<KeyValuePair<K, V>>.Add(KeyValuePair<K, V> item) => Add(item.Key, item.Value);

		bool ICollection<KeyValuePair<K, V>>.Contains(KeyValuePair<K, V> item) => this.As<IDictionary<K, V>>().TryGetValue(item.Key, out var foundValue) && Equals(foundValue, item.Value);
		bool IReadOnlyDictionary<K, V>.ContainsKey(K key) => this.As<IReadOnlyDictionary<K, V>>().TryGetValue(key, out _);
		bool IDictionary<K, V>.ContainsKey(K key) => this.As<IDictionary<K, V>>().TryGetValue(key, out _);
		public bool ContainsValue(V value, IEqualityComparer<V> equalityComparer = null)
		{
			var values = Values;
			return equalityComparer == null ? values.Contains(value) : values.ContainsValue(value, equalityComparer);
		}

		bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item) => this.As<IDictionary<K, V>>().TryGetValue(item.Key, out var existingValue)
			&& Equals(item.Value, existingValue) && Remove(item.Key);
	}
}
