using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Purchasing.Editor.Shared.UI;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using ILogger = UnityEditor.Purchasing.Editor.Authoring.Core.Logger.ILogger;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    class RoutingDeleteRemoteCommand : Command
    {
        readonly IRoutingClient m_Client;
        readonly IEnvironmentsApi m_EnvironmentsApi;
        readonly IDisplayDialog m_Dialog;
        readonly ILogger m_Logger;

        public override string Name => L10n.Tr("Delete Remote");

        public RoutingDeleteRemoteCommand(
            IRoutingClient client,
            IEnvironmentsApi environmentsApi,
            IDisplayDialog dialog,
            ILogger logger)
        {
            m_Client = client;
            m_EnvironmentsApi = environmentsApi;
            m_Dialog = dialog;
            m_Logger = logger;
        }

        public override async Task ExecuteAsync(
            IEnumerable<IDeploymentItem> items,
            CancellationToken cancellationToken = default)
        {
            var itemList = new List<IDeploymentItem>(items);

            var dialogResult = m_Dialog.Show(
                "Clear Routing Config",
                "Are you sure you want to clear the remote routing configuration? " +
                "This will remove all tag-to-provider mappings.",
                "Yes",
                "Cancel");

            if (!dialogResult)
            {
                return;
            }

            var envId = m_EnvironmentsApi.ActiveEnvironmentId.ToString();
            var projectId = CloudProjectSettings.projectId;
            await m_Client.Initialize(envId, projectId, cancellationToken);

            try
            {
                var emptyConfig = new ProviderRoutingConfig();
                await m_Client.PushRouting(emptyConfig, cancellationToken);
                m_Logger.LogInfo("Remote routing configuration cleared.");

                foreach (var item in itemList)
                {
                    item.Status = DeploymentStatus.Empty;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                m_Logger.LogError(exception);

                foreach (var item in itemList)
                {
                    item.Status = new DeploymentStatus("Failed to delete", exception.Message, SeverityLevel.Error);
                }
            }
        }
    }
}
