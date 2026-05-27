using UnityEngine;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    static class XboxSettingsLoader
    {
        internal const string k_AssetPath = "Assets/Resources/XboxCloudSettings.asset";
        const string k_ResourcesPath = "Assets/Resources";

        internal static XboxCloudSettings LoadOrCreate()
        {
            var settings = Resources.Load<XboxCloudSettings>(XboxCloudSettings.k_AssetName);
            if (settings != null)
            {
                return settings;
            }

            if (!AssetDatabase.IsValidFolder(k_ResourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            settings = ScriptableObject.CreateInstance<XboxCloudSettings>();
            AssetDatabase.CreateAsset(settings, k_AssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}
