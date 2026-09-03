using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Purchasing.Editor.Shared.Logging;

namespace UnityEditor.Purchasing.Editor.Authoring.LiveContentAdminApi
{
    public static class InlineVariantFormat
    {
        public const string HeaderFeatureFlagKey = "X-Feature-Flag";
        public const string HeaderFeatureFlagValue = "file-repo-v2";

        static bool ItemHasVariants(JToken item)
        {
            return item is JObject obj
                && obj["variants"] is JArray { Count: > 0 };
        }

        static bool FirstItemHasVariants(JToken root)
        {
            return root is JArray { Count: > 0 } array
                && ItemHasVariants(array[0]);
        }

        static bool HasInlineVariant(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            try
            {
                var root = JToken.Parse(content);
                return FirstItemHasVariants(root) || ItemHasVariants(root);
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }

        public static void LogErrorIfDetected(string content)
        {
            if (HasInlineVariant(content))
            {
                Logger.LogError(
                    "Catalog and Webshop management requires a newer In App Purchase SDK. " +
                    "Runtime delivery is unaffected.");
            }
        }
    }
}
