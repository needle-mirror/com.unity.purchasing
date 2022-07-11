using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Purchasing
{
    static class EnumerableExtensions
    {
        public static IEnumerable<T> NonNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.Where(obj => obj != null);
        }

        public static IEnumerable<T> IgnoreExceptions<T, TException>(this IEnumerable<T> enumerable,
            Action<TException> onException = null) where TException : Exception
        {
            using var enumerator = enumerable.GetEnumerator();

            var hasNext = true;

            while (hasNext)
            {
                try
                {
                    hasNext = enumerator.MoveNext();
                }
                catch (TException ex)
                {
                    onException?.Invoke(ex);
                    continue;
                }

                if (hasNext)
                {
                    yield return enumerator.Current;
                }
            }
        }
    }
}
