using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    class RoutingDeploymentProvider : DeploymentProvider
    {
        public override string Service => L10n.Tr("Payment Provider Routing");
        public override Command DeployCommand { get; }

        public RoutingDeploymentProvider(
            RoutingDeployCommand deployCommand,
            RoutingDeleteRemoteCommand deleteRemoteCommand,
            RoutingOpenDashboardCommand openDashboardCommand,
            ObservableRoutingAssets routingAssets)
            : base(routingAssets.DeploymentItems)
        {
            DeployCommand = deployCommand;
            Commands.Add(deleteRemoteCommand);
            Commands.Add(openDashboardCommand);
        }
    }
}
