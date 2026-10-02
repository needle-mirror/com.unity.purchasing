using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    class PurchasingDeploymentProvider : DeploymentProvider
    {
        public override string Service => L10n.Tr("Purchasing");
        public override Command DeployCommand { get; }
        public override Command SyncItemsWithRemoteCommand { get; }
        public Command DeleteRemoteCommand { get; }

        public PurchasingDeploymentProvider(
            DeployCommandWrapper deployCommandWrapper,
            DeleteRemoteCommandWrapper deleteRemoteCommandWrapper,
            CatalogOpenDashboardCommand openDashboardCommand,
            SyncItemsWithRemoteCommand syncCommand,
            ObservableCatalogItemAssets ucatAssets,
            ObservableCatalogCsvAssets csvAssets)
        {
            DeployCommand = deployCommandWrapper;
            DeleteRemoteCommand = deleteRemoteCommandWrapper;
            SyncItemsWithRemoteCommand = syncCommand;
            Commands.Add(openDashboardCommand);
            Commands.Add(DeleteRemoteCommand);

            DeploymentItemForwarder.Forward(ucatAssets.DeploymentItems, DeploymentItems);
            DeploymentItemForwarder.Forward(csvAssets.DeploymentItems, DeploymentItems);
        }
    }
}
