// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Unity.Purchasing.Editor.Shared.Infrastructure.Collections
{
    sealed class MergedObservableCollection<T> : IReadOnlyObservable<T>
    {
        readonly IList<IReadOnlyObservable<T>> m_MergedCollections = new List<IReadOnlyObservable<T>>();

        public int Count => m_MergedCollections.Select(l => l.Count).Sum();
        public T this[int index] => Index(index);
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public MergedObservableCollection(IEnumerable<IReadOnlyObservable<T>> collections)
        {
            foreach (var collection in collections)
            {
                AddCollection(collection);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_MergedCollections.SelectMany(l => l).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void AddCollection(IReadOnlyObservable<T> item)
        {
            m_MergedCollections.Add(item);
            item.CollectionChanged += SubItemOnCollectionChanged;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void RemoveCollectionAt(int index)
        {
            m_MergedCollections[index].CollectionChanged -= SubItemOnCollectionChanged;
            m_MergedCollections.RemoveAt(index);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void ClearCollections()
        {
            foreach (var collection in m_MergedCollections)
            {
                collection.CollectionChanged -= SubItemOnCollectionChanged;
            }
            m_MergedCollections.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Dispose()
        {
            foreach (var observable in m_MergedCollections)
            {
                observable.CollectionChanged -= SubItemOnCollectionChanged;
                observable.Dispose();
            }
        }

        T Index(int index)
        {
            var target = index;
            foreach (var collection in m_MergedCollections)
            {
                if (collection.Count > target)
                {
                    return collection[target];
                }
                target -= collection.Count;
            }

            throw new IndexOutOfRangeException($"{index} exceeds the collection bounds");
        }

        /// <summary>
        /// Combines all NotifyCollectionChangedEventArgs args into a single event
        /// This does not remap indexes so the order of items based on NotifyCollectionChangedEventArgs is lost within ObservableMerge
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SubItemOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
    }
}
