using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Deploy;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    class DeployCommand : Command<CatalogEntryDeploymentItem>
    {
        readonly ICatalogDeploymentHandler m_DeploymentHandler;
        readonly ILiveContentConfigClient m_Client;
        readonly IWebshopCategoriesClient m_CategoriesClient;
        readonly IEnvironmentsApi m_EnvironmentsApi;

        public override string Name => L10n.Tr("Deploy");

        public DeployCommand(
            ICatalogDeploymentHandler moduleDeploymentHandler,
            ILiveContentConfigClient client,
            IWebshopCategoriesClient categoriesClient,
            IEnvironmentsApi environmentsApi)
        {
            m_DeploymentHandler = moduleDeploymentHandler;
            m_Client = client;
            m_CategoriesClient = categoriesClient;
            m_EnvironmentsApi = environmentsApi;
        }

        public override async Task ExecuteAsync(IEnumerable<CatalogEntryDeploymentItem> items, CancellationToken cancellationToken = default)
        {
            var itemList = items.ToList();
            OnPreDeploy(itemList);

            if (!await TryInitializeClients(itemList, cancellationToken))
                return;

            await m_DeploymentHandler.DeployAsync(itemList, false, false, cancellationToken);
        }

        /// <summary>
        /// Initialization rejects a missing project or environment id. Report that on the items,
        /// otherwise they would keep whatever status they had before the deploy.
        /// </summary>
        async Task<bool> TryInitializeClients(
            IReadOnlyList<CatalogEntryDeploymentItem> items,
            CancellationToken cancellationToken)
        {
            try
            {
                var envId = m_EnvironmentsApi.ActiveEnvironmentId.ToString();
                await m_Client.Initialize(envId, CloudProjectSettings.projectId, cancellationToken);
                await m_CategoriesClient.Initialize(envId, CloudProjectSettings.projectId, cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                MarkFailed(items, e.Message);
                return false;
            }
        }

        static void OnPreDeploy(IReadOnlyList<CatalogEntryDeploymentItem> items)
        {
            foreach (var i in items)
            {
                i.Progress = 0f;
                i.Status = new DeploymentStatus();
                i.States.Clear();
            }
        }

        static void MarkFailed(IReadOnlyList<CatalogEntryDeploymentItem> items, string message)
        {
            foreach (var i in items)
            {
                i.Status = Statuses.GetFailedToDeploy(message);
                i.Progress = 100f;
            }
        }
    }
}
