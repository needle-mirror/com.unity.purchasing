using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Utils;

namespace UnityEngine.Purchasing.Models
{
    static class AndroidJavaObjectExtensions
    {
        internal static IEnumerable<T> Enumerate<T>(this AndroidJavaObject androidJavaList)
        {
            var size = androidJavaList?.Call<int>("size") ?? 0;
            return Enumerable.Range(0, size).Select(i => androidJavaList.Call<T>("get", i));
        }

        internal static IEnumerable<IAndroidJavaObjectWrapper> EnumerateAndWrap(this AndroidJavaObject androidJavaList)
        {
            return androidJavaList.Enumerate<AndroidJavaObject>().Wrap();
        }

        internal static IEnumerable<IAndroidJavaObjectWrapper> Wrap(this IEnumerable<AndroidJavaObject> androidJavaList)
        {
            return androidJavaList.Select(javaObject => javaObject.Wrap());
        }

        internal static IAndroidJavaObjectWrapper Wrap(this AndroidJavaObject androidJavaObject)
        {
            return new AndroidJavaObjectWrapper(androidJavaObject);
        }
    }
}
