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
            if (deploymentItem == null)
            {
                return;
            }

            var provider = PurchasingAuthoringServiceProvider.GetService<DeploymentProvider>();
            var footer = new DeploymentConfigInspectorFooter();
            footer.BindGUI(
                assetPath,
                PurchasingAuthoringServiceProvider.GetService<ICommonAnalytics>(),
                provider.Commands,
                deploymentItem,
                "Purchasing");
            container.Add(footer);
        }
    }
}
