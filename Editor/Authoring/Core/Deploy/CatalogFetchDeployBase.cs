using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Logger;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using UnityEditor.Purchasing.Editor.Authoring.Core.Validations;


namespace UnityEditor.Purchasing.Editor.Authoring.Core.Deploy
{
    abstract class CatalogFetchDeployBase
    {
        IReadOnlyDictionary<string, CatalogEntryDeploymentItem> m_LocalMap;
        IReadOnlyDictionary<string, CatalogEntryDeploymentItem> m_RemoteMap;
        protected ILiveContentConfigClient Client { get; }
        protected ILogger Logger { get; }

        protected CatalogFetchDeployBase(ILiveContentConfigClient client, ILogger logger)
        {
            Client = client;
            Logger = logger;
        }

        protected void SetupMaps(IReadOnlyList<CatalogEntryDeploymentItem> filteredLocalResources, IReadOnlyList<CatalogEntryDeploymentItem> remoteResources)
        {
            m_LocalMap = filteredLocalResources.ToDictionary(l => l.CatalogItem.CatalogListingId, l => l);
            m_RemoteMap = remoteResources.ToDictionary(l => l.CatalogItem.CatalogListingId, l => l);
        }

        protected async Task<IReadOnlyList<CatalogEntryDeploymentItem>> GetRemoteItems(
            IReadOnlyList<CatalogEntryDeploymentItem> localResources,
            string rootDirectory = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var remoteResources = await Client.List(cancellationToken: cancellationToken);
                var deploymentItems = remoteResources.Select(ci =>
                    new CatalogEntryDeploymentItem("Remote") { CatalogItem = ci, }
                ).ToList();
                return deploymentItems;
            }
            catch (ClientException e)
            {
                foreach (var catalogEntryDeploymentItem in localResources)
                {
                    catalogEntryDeploymentItem.Status = DeploymentStatus.GetFailedToDeploy(
                        $"Failed to deploy item. An error happened while communicating with the server. {e.Message}"
                        );
                }
                Logger.LogError(e);
                throw;
            }
        }

        protected bool ExistsRemotely(CatalogEntryDeploymentItem catalogEntryDeploymentItem)
        {
            return m_RemoteMap.ContainsKey(catalogEntryDeploymentItem.CatalogItem.CatalogListingId);
        }

        protected bool DoesNotExistRemotely(CatalogEntryDeploymentItem catalogEntryDeploymentItem)
        {
            return !m_RemoteMap.ContainsKey(catalogEntryDeploymentItem.CatalogItem.CatalogListingId);
        }

        protected bool DoesNotExistLocally(CatalogEntryDeploymentItem catalogEntryDeploymentItem)
        {
            return !m_LocalMap.ContainsKey(catalogEntryDeploymentItem.CatalogItem.CatalogListingId);
        }

        protected CatalogEntryDeploymentItem GetRemoteResourceItem(string id)
        {
            return m_RemoteMap[id];
        }

        protected async Task DeployResource(
            Func<CatalogItem, CancellationToken, Task> task,
            CatalogEntryDeploymentItem catalogEntryDeploymentItem,
            string taskAction,
            CancellationToken token)
        {
            try
            {
                catalogEntryDeploymentItem.Status = Statuses.GetDeploying();
                await task(catalogEntryDeploymentItem.CatalogItem, token);
                catalogEntryDeploymentItem.Status = Statuses.GetDeployed(taskAction);
                catalogEntryDeploymentItem.Progress = 100f;
            }
            catch (ClientException e)
            {
                Logger.LogVerbose(e);
                catalogEntryDeploymentItem.Status = Statuses.GetFailedToDeploy(e.Message);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                catalogEntryDeploymentItem.Status = Statuses.GetFailedToDeploy(e.ToString());
            }
        }

        protected void UpdateDuplicateResourceStatus(
            IReadOnlyList<IGrouping<string, CatalogEntryDeploymentItem>> duplicateGroups)
        {
            foreach (var group in duplicateGroups)
            {
                foreach (var resourceItem in group)
                {
                    var(message, shortMessage) = DuplicateResourceValidation.GetDuplicateResourceErrorMessages(resourceItem, group.ToList());
                    resourceItem.Status = GetFailedStatus(shortMessage);
                }
            }
        }

        protected virtual CatalogEntryDeploymentItem CreateItem(string rootDirectory, CatalogItem catalogItem)
        {
            var path = rootDirectory != null
                ? Path.Combine(rootDirectory, catalogItem.CatalogListingId + Constants.FileExtension)
                : "Remote";
            return new CatalogEntryDeploymentItem(path)
            {
                CatalogItem = catalogItem
            };
        }

        protected virtual void UpdateDryRunResult(
            IReadOnlyList<CatalogEntryDeploymentItem> toUpdate,
            IReadOnlyList<CatalogEntryDeploymentItem> toDelete,
            IReadOnlyList<CatalogEntryDeploymentItem> toCreate)
        {
            foreach (var i in toUpdate)
            {
                i.Status = GetSuccessStatus(Constants.Updated);
            }

            foreach (var i in toDelete)
            {
                i.Status = GetSuccessStatus(Constants.Deleted);
            }

            foreach (var i in toCreate)
            {
                i.Status = GetSuccessStatus(Constants.Created);
            }
        }

        protected abstract DeploymentStatus GetSuccessStatus(string message);

        protected abstract DeploymentStatus GetFailedStatus(string message, IReadOnlyList<IDeploymentItem> failedItems = null);

        protected virtual DeploymentStatus GetPartialStatus(string message, IReadOnlyList<IDeploymentItem> failedItems = null)
        {
            var str = GetNestedDetails(failedItems);
            return Statuses.GetPartialDeploy(str);
        }

        protected static string GetNestedDetails(IReadOnlyList<IDeploymentItem> failedItems)
        {
            if (failedItems == null || failedItems.Count == 0)
                return string.Empty;
            var strBuild = new StringBuilder();
            strBuild.AppendLine("Failed items:");
            foreach (var nested in failedItems)
            {
                var status = nested.Status.Message;
                string detail = "";
                if (!string.IsNullOrEmpty(nested.Status.MessageDetail))
                    detail = $" - {nested.Status.MessageDetail}";
                strBuild.AppendLine($"  - {nested.Name}: {status}{detail}");
            }

            var str = strBuild.ToString();
            return str;
        }
    }
}
