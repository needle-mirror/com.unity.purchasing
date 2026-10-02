using System.IO;
using Newtonsoft.Json;
using Unity.Purchasing.Editor.Shared.Assets;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEngine;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
    class RoutingAsset : ScriptableObject, IPath, ISerializationCallbackReceiver
    {
        const string k_DefaultFileName = "MyRoutingConfig";

        string m_Path;

        public string Name { get; set; }
        public string Path { get => m_Path; set => SetPath(value); }
        public RoutingDeploymentItem RoutingDeploymentItem { get; set; }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize() { Name = System.IO.Path.GetFileNameWithoutExtension(Path); }

        void SetPath(string path)
        {
            RoutingDeploymentItem ??= new RoutingDeploymentItem(path);

            m_Path = path;
            Name = System.IO.Path.GetFileNameWithoutExtension(path);

            RoutingDeploymentItem.Path = path;
            RoutingDeploymentItem.Name = System.IO.Path.GetFileName(path);
        }

        [MenuItem("Assets/Create/Services/IAP Routing Config", false, 82)]
        public static void CreateConfig()
        {
            var folder = CatalogAssetHelper.GetActiveFolderPath();
            var path = CatalogAssetHelper.GenerateUniquePath(folder, k_DefaultFileName, Constants.RoutingFileExtension);

            var endAction = CreateInstance<CreateRoutingAssetAction>();
#if UNITY_6000_4_OR_NEWER
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(default(UnityEngine.EntityId), endAction, path, null, null);
#else
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endAction, path, null, null);
#endif
        }

        public void SaveToDisk()
        {
            var serializedContent = JsonConvert.SerializeObject(
                RoutingDeploymentItem.RoutingConfig,
                Formatting.Indented);
            File.WriteAllText(RoutingDeploymentItem.Path, serializedContent);
        }
    }
}
