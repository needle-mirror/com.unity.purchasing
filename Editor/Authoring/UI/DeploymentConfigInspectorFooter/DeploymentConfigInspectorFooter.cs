using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Purchasing.Editor.Shared.Analytics;
using Unity.Services.DeploymentApi.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.UI.DeploymentConfigInspectorFooter
{
#if UNITY_2023_3_OR_NEWER
    [UxmlElement]
#endif
    partial class DeploymentConfigInspectorFooter : BindableElement
    {
        const string k_ViewInDeploymentAnalyticsKey = "clicked_view_in_deployment_window_btn";
        const string k_CommandKeyFormat = "clicked_command_btn_{0}";
        string m_ServiceName;
        string m_FilePath;
        ICommonAnalytics m_CommonAnalyticsSender;

        public void BindGUI(
            string filePath,
            ICommonAnalytics analyticsSender,
            IList<Command> commands,
            IDeploymentItem deploymentItem,
            string serviceName = "")
        {
            m_ServiceName = serviceName ?? ReadPackageInfo().displayName;
            m_FilePath = filePath;
            m_CommonAnalyticsSender = analyticsSender;
            SetupFooterVisual();
            SetupBtnViewInDeploymentWindow(this);
            SetupBtnsCommands(this, commands, deploymentItem);
        }

        public void BindCommand(Command command, IDeploymentItem item)
        {
            var container = this.Q<VisualElement>("deployment-container");
            var entry = new VisualElement();
            entry.AddToClassList("deployment-content-container");
            var button = new Button(async() =>
            {
                await command.ExecuteAsync(new[] { item });
                SendAnalyticsEvent(string.Format(k_CommandKeyFormat, command.Name));
            });
            button.text = command.Name;
            button.SetEnabled(command.IsEnabled(new[] {item}));
            button.visible = command.IsVisible(new[] { item });
            entry.Add(button);
            container.Add(entry);
        }

        void SetupFooterVisual([CallerFilePath] string sourceFilePath = "")
        {
            var basePath = GetBasePath(sourceFilePath);
            var uxmlPath = Path.Combine(basePath, "DeploymentConfigInspectorFooter.uxml");
            var ussPath = Path.Combine(basePath, "DeploymentConfigInspectorFooter.uss");
            var ussDarkPath = Path.Combine(basePath, "DeploymentConfigInspectorFooterDark.uss");
            var ussLightPath = Path.Combine(basePath, "DeploymentConfigInspectorFooterLight.uss");

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath));
            styleSheets.Add(
                EditorGUIUtility.isProSkin ? AssetDatabase.LoadAssetAtPath<StyleSheet>(ussDarkPath) :
                AssetDatabase.LoadAssetAtPath<StyleSheet>(ussLightPath));

            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            visualTreeAsset.CloneTree(this);
        }

        string GetBasePath(string sourceFilePath)
        {
            var packageInfo = ReadPackageInfo();
            var dirFullPath = Path.GetFullPath(Path.GetDirectoryName(sourceFilePath) !);
            var editorIx = dirFullPath.IndexOf("Editor");
            var dirRelativePath = dirFullPath.Substring(editorIx);

            var basePath = Path.Combine("Packages",
                packageInfo.name,
                dirRelativePath,
"Assets");
            return basePath;
        }

        UnityEditor.PackageManager.PackageInfo ReadPackageInfo()
        {
            return UnityEditor.PackageManager.PackageInfo.FindForAssembly(GetType().Assembly);
        }

        void SetupBtnViewInDeploymentWindow(VisualElement myInspector)
        {
            var viewInDeployBtn = myInspector.Q<Button>("view-in-deployment-window-btn");
            if (viewInDeployBtn != null)
            {
                viewInDeployBtn.clickable.clicked += SelectFileInDeploymentWindow;
            }
        }

        void SetupBtnsCommands(VisualElement myInspector, IList<Command> commands, IDeploymentItem deploymentItem)
        {
            foreach (var command in commands)
            {
                BindCommand(command, deploymentItem);
            }
        }

        void SelectFileInDeploymentWindow()
        {
            if (File.Exists(m_FilePath))
            {
#if DEPLOYMENT_API_AVAILABLE_V1_1
                Deployments.Instance.DeploymentWindow.OpenWindow();
                Deployments.Instance.DeploymentWindow.ClearSelection();

                var deploymentItems = GetDeploymentItems();
                Deployments.Instance.DeploymentWindow.Select(deploymentItems);
                SendAnalyticsEvent(k_ViewInDeploymentAnalyticsKey);
#elif DEPLOYMENT_API_AVAILABLE_V1_0
                Logging.Logger.Log("Please update your Deployment package to use this feature. A minimum version of 1.4.0 is required.");
#endif
            }
        }

#if DEPLOYMENT_API_AVAILABLE_V1_1
        List<IDeploymentItem> GetDeploymentItems()
        {
            var deploymentItems = Deployments.Instance.DeploymentWindow.GetFromFiles(new List<string> { m_FilePath });

            var simpleItems = deploymentItems.FindAll(x => x is not ICompositeItem);
            var compositeItems = deploymentItems.FindAll(x => x is ICompositeItem);

            // if any item is composite unwinds its children and add to the list
            if (compositeItems.Any())
            {
                foreach (var item in compositeItems)
                {
                    simpleItems.AddRange(((ICompositeItem)item).Children);
                }
            }

            return simpleItems;
        }

#endif
        void SendAnalyticsEvent(string key)
        {
            m_CommonAnalyticsSender.Send(new ICommonAnalytics.CommonEventPayload
            {
                action = key,
                context = m_ServiceName
            });
        }

#if !UNITY_2023_3_OR_NEWER
        new class UxmlFactory : UxmlFactory<DeploymentConfigInspectorFooter> {}
#endif
    }
}
