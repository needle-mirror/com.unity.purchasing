using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Deploy;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    class RoutingDeployCommand : Command<RoutingDeploymentItem>
    {
        readonly IRoutingDeploymentHandler m_DeploymentHandler;
        readonly IRoutingClient m_Client;
        readonly IEnvironmentsApi m_EnvironmentsApi;

        public override string Name => L10n.Tr("Deploy");

        public RoutingDeployCommand(
            IRoutingDeploymentHandler deploymentHandler,
            IRoutingClient client,
            IEnvironmentsApi environmentsApi)
        {
            m_DeploymentHandler = deploymentHandler;
            m_Client = client;
            m_EnvironmentsApi = environmentsApi;
        }

        public override async Task ExecuteAsync(
            IEnumerable<RoutingDeploymentItem> items,
            CancellationToken cancellationToken = default)
        {
            var itemList = new List<RoutingDeploymentItem>(items);
            if (itemList.Count == 0)
            {
                return;
            }

            if (itemList.Count > 1)
            {
                foreach (var rejected in itemList)
                {
                    rejected.Status = new DeploymentStatus(
                        "Failed to deploy",
                        "Only one routing configuration can be deployed at a time.",
                        SeverityLevel.Error);
                }

                return;
            }

            var item = itemList[0];
            OnPreDeploy(item);

            var envId = m_EnvironmentsApi.ActiveEnvironmentId.ToString();
            var projectId = CloudProjectSettings.projectId;
            await m_Client.Initialize(envId, projectId, cancellationToken);

            await m_DeploymentHandler.DeployAsync(item, dryRun: false, cancellationToken);
        }

        static void OnPreDeploy(RoutingDeploymentItem item)
        {
            item.Progress = 0f;
            item.Status = new DeploymentStatus();
            item.States.Clear();
        }
    }
}
