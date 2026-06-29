// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Unity.Purchasing.Editor.Shared.Infrastructure.Collections
{
    sealed class ReadOnlyObservableCollection<T> : IReadOnlyObservable<T>
    {
        readonly ObservableCollection<T> m_Collection;

        public int Count => m_Collection.Count;
        public T this[int index] => m_Collection[index];
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => m_Collection.CollectionChanged += value;
            remove => m_Collection.CollectionChanged -= value;
        }
        public IEnumerator<T> GetEnumerator() => m_Collection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public ReadOnlyObservableCollection(ObservableCollection<T> collection)
        {
            m_Collection = collection;
        }

        public void Dispose()
        {
            // no internal events
        }
    }
}
