using System;
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

            if (dryRun)
            {
                item.Status = Statuses.GetDeployed("Would deploy");
                item.Progress = 100;
                return result;
            }

            item.Progress = 50;
            item.Status = Statuses.GetDeploying();

            try
            {
                await m_Client.PushRouting(item.RoutingConfig, token);
                item.Progress = 100;
                item.Status = Statuses.GetDeployed("Updated");
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
