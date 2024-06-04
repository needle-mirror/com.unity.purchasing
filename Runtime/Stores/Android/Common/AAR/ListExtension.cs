using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    static class ListExtension
    {
        internal static AndroidJavaObject ToJava<T>(this List<T> values)
        {
            return ToJavaArray(values);
        }

        static AndroidJavaObject ToJavaArray<T>(List<T> values)
        {
            var list = new AndroidJavaObject("java.util.ArrayList");
            foreach (var value in values)
            {
                list.Call<bool>("add", value);
            }
            return list;
        }
    }
}
