using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Purchasing.Editor.Authoring.Core.Logger;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Deploy
{
    class RoutingDeploymentHandler : IRoutingDeploymentHandler
    {
        readonly IRoutingClient m_Client;
        readonly ILogger m_Logger;

        public RoutingDeploymentHandler(IRoutingClient client, ILogger logger)
        {
            m_Client = client;
            m_Logger = logger;
        }

        public async Task<DeployResult> DeployAsync(
            RoutingDeploymentItem item,
            bool dryRun = false,
            CancellationToken token = default)
        {
            var result = new DeployResult { Deployed = new[] { item } };

            if (!item.Validate())
            {
                item.Status = Statuses.GetFailedToDeploy("Routing config is invalid and will not be deployed");
                return result;
            }

            item.Progress = 50;
            item.Status = Statuses.GetDeploying();

            try
            {
                // A deploy replaces the whole mapping, and other writers (Campaigns) add their own tags,
                // so say which tags on the server the file would remove.
                var remote = await m_Client.FetchRouting(token);
                var local = item.RoutingConfig.Mappings;
                var removed = (remote?.Mappings?.Keys ?? Enumerable.Empty<string>())
                    .Where(tag => local == null || !local.ContainsKey(tag))
                    .OrderBy(tag => tag, StringComparer.Ordinal)
                    .ToList();

                if (!dryRun)
                {
                    await m_Client.PushRouting(item.RoutingConfig, token);
                }

                // Only after the push, so a failed push never reports tags as removed.
                var removedNote = removed.Count == 0
                    ? string.Empty
                    : $" Tags on the server but not in this file {(dryRun ? "would be" : "were")} removed: {string.Join(", ", removed)}.";
                if (removed.Count > 0)
                {
                    m_Logger.LogWarning($"Routing deploy of '{item.Path}':{removedNote}");
                }

                item.Progress = 100;
                item.Status = Statuses.GetDeployed((dryRun ? "Would deploy." : "Updated.") + removedNote);
            }
            catch (OperationCanceledException)
            {
                item.Progress = 0;
                item.Status = Statuses.GetFailedToDeploy("Cancelled");
                throw;
            }
            catch (ClientException exception)
            {
                item.Progress = 0;
                item.Status = Statuses.GetFailedToDeploy(
                    $"Failed to deploy routing config. An error happened while communicating with the server. {exception.Message}");
                m_Logger.LogError(exception);
            }
            catch (Exception exception)
            {
                item.Progress = 0;
                item.Status = Statuses.GetFailedToDeploy(
                    $"Failed to deploy routing config. An unexpected error happened. Reason: {exception.Message}");
                m_Logger.LogError(exception);
            }

            return result;
        }
    }
}
