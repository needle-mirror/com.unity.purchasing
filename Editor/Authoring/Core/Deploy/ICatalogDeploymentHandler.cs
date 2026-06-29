using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Deploy
{
    interface ICatalogDeploymentHandler
    {
        Task<DeployResult> DeployAsync(
            IReadOnlyList<CatalogEntryDeploymentItem> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default);
    }
}
