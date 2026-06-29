// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Unity.Purchasing.Editor.Shared.Infrastructure.Collections
{
    sealed class MappedObservableCollection<TCollection, TFrom, TTo> : IReadOnlyObservable<TTo> where TCollection : IReadOnlyList<TFrom>, INotifyCollectionChanged
    {
        readonly ObservableCollection<TTo> m_Collection;
        readonly TCollection m_RootCollection;
        readonly Func<TFrom, TTo> m_Mapping;

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => m_Collection.CollectionChanged += value;
            remove => m_Collection.CollectionChanged -= value;
        }
        public int Count => m_Collection.Count;
        public TTo this[int index] => m_Collection[index];

        public MappedObservableCollection(TCollection root, Func<TFrom, TTo> mapping)
        {
            m_RootCollection = root;
            m_Mapping = mapping;

            m_Collection = new ObservableCollection<TTo>(root.Select(mapping));
            m_RootCollection.CollectionChanged += RootOnCollectionChanged;
        }

        public IEnumerator<TTo> GetEnumerator()
        {
            return m_Collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            foreach (var existing in m_Collection)
            {
                TryDispose(existing);
            }
            m_RootCollection.CollectionChanged -= RootOnCollectionChanged;
        }

        void RootOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs original)
        {
            switch (original.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (original.NewItems.Count != 1)
                    {
                        throw new InvalidOperationException("Unable to add more than one value");
                    }
                    m_Collection.Insert(original.NewStartingIndex, m_Mapping((TFrom)original.NewItems[0]));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (original.OldItems.Count != 1)
                    {
                        throw new InvalidOperationException("Unable to remove more than one value");
                    }
                    TryDispose(m_Collection[original.OldStartingIndex]);
                    m_Collection.RemoveAt(original.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Move:
                    m_Collection.Move(original.OldStartingIndex, original.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (original.NewItems.Count != 1)
                    {
                        throw new InvalidOperationException("Unable to replace more than one value");
                    }
                    TryDispose(m_Collection[original.NewStartingIndex]);
                    m_Collection[original.NewStartingIndex] = m_Mapping((TFrom)original.NewItems[0]);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var existing in m_Collection)
                    {
                        TryDispose(existing);
                    }
                    m_Collection.Clear();
                    foreach (var item in m_RootCollection)
                    {
                        m_Collection.Add(m_Mapping(item));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unsupported action: {original.Action}");
            }
        }

        static void TryDispose(TTo item)
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
