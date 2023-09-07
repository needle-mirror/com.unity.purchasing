using System.Collections.Generic;
using Unity.Services.Core.Editor.OrganizationHandler;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    static class NetworkingUtils
    {
        internal static string GetStringFromRawJsonDictionaryString(string rawJson, string key)
        {
            var container = (Dictionary<string, object>)MiniJson.JsonDecode(rawJson);

            return GetStringFromJsonDictionary(container, key);
        }

        internal static string GetStringFromJsonDictionary(Dictionary<string, object> container, string key)
        {
            object value = null;
            container?.TryGetValue(key, out value);
            return value as string;
        }

        internal static Dictionary<string, object> GetJsonDictionaryWithinRawJsonDictionaryString(string rawJson, string key)
        {
            var container = (Dictionary<string, object>)MiniJson.JsonDecode(rawJson);

            return GetJsonDictionaryWithinJsonDictionary(container, key);
        }


        static Dictionary<string, object> GetJsonDictionaryWithinJsonDictionary(Dictionary<string, object> container, string key)
        {
            object value = null;
            container?.TryGetValue(key, out value);
            return value as Dictionary<string, object>;
        }
    }
}
