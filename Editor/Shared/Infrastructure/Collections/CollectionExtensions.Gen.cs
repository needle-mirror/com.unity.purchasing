// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Unity.Purchasing.Editor.Shared.Infrastructure.Collections
{
    static class CollectionExtensions
    {
        public static IReadOnlyObservable<TTo> Map<TFrom, TTo>(this ObservableCollection<TFrom> collection, Func<TFrom, TTo> mapping)
        {
            return new MappedObservableCollection<ObservableCollection<TFrom>, TFrom, TTo>(collection, mapping);
        }

        public static IReadOnlyObservable<T> AsReadonly<T>(this ObservableCollection<T> collection)
        {
            return new ReadOnlyObservableCollection<T>(collection);
        }

        public static IReadOnlyObservable<T> Merge<T>(this IEnumerable<IReadOnlyObservable<T>> collections)
        {
            return new MergedObservableCollection<T>(collections);
        }

        public static IReadOnlyCollection<T> EnumerateOnce<T>(this IEnumerable<T> enumerable)
        {
            var res = enumerable as IReadOnlyCollection<T>;
            return res ?? enumerable.ToList();
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }

        public static IReadOnlyList<TOut> ForEach<T, TOut>(this IEnumerable<T> enumerable, Func<T, TOut> func)
        {
            List<TOut> res = new List<TOut>();
            foreach (var item in enumerable)
            {
                res.Add(func(item));
            }

            return res;
        }
    }
}
