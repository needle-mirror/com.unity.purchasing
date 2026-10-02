using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Deploy
{
    interface IRoutingDeploymentHandler
    {
        Task<DeployResult> DeployAsync(
            RoutingDeploymentItem item,
            bool dryRun = false,
            CancellationToken token = default);
    }
}
