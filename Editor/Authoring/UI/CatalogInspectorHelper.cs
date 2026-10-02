using Unity.Purchasing.Editor.Shared.Analytics;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring;
using UnityEditor.Purchasing.UI.DeploymentConfigInspectorFooter;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Editor.Authoring.UI
{
    static class CatalogInspectorHelper
    {
        public static void AddDeploymentFooter(
            VisualElement container, string assetPath, IDeploymentItem deploymentItem)
        {
            var provider = PurchasingAuthoringServiceProvider.GetService<DeploymentProvider>();
            AddDeploymentFooter(container, assetPath, deploymentItem, provider, "Purchasing");
        }

        public static void AddDeploymentFooter(
            VisualElement container, string assetPath, IDeploymentItem deploymentItem,
            DeploymentProvider provider, string serviceName)
        {
            if (deploymentItem == null)
            {
                return;
            }

            var footer = new DeploymentConfigInspectorFooter();
            footer.BindGUI(
                assetPath,
                PurchasingAuthoringServiceProvider.GetService<ICommonAnalytics>(),
                provider.Commands,
                deploymentItem,
                serviceName);
            container.Add(footer);
        }

        public static bool AreSerializedObjectsEqual(SerializedObject first, SerializedObject second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            var iteratorFirst = first.GetIterator();
            var iteratorSecond = second.GetIterator();

            while (iteratorFirst.NextVisible(true) && iteratorSecond.NextVisible(true))
            {
                if (iteratorFirst.propertyType != iteratorSecond.propertyType
                    || iteratorFirst.name != iteratorSecond.name)
                {
                    return false;
                }

                if (!SerializedProperty.DataEquals(iteratorFirst, iteratorSecond))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
