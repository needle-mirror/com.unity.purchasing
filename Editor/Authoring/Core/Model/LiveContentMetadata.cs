using Newtonsoft.Json.Linq;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    internal static class LiveContentMetadata
    {
        const string k_ManagedByKey = "managedBy";
        const string k_ManagedByValue = "In App Purchase";

        internal static JObject ApplyManagedBy(JObject metadata)
        {
            metadata ??= new JObject();
            metadata[k_ManagedByKey] = k_ManagedByValue;
            return metadata;
        }
    }
}
