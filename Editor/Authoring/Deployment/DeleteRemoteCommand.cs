using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Purchasing.Editor.Shared.UI;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using UnityEngine;

using ILogger = UnityEditor.Purchasing.Editor.Authoring.Core.Logger.ILogger;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    class DeleteRemoteCommand : Command<CatalogEntryDeploymentItem>
    {
        readonly ILiveContentConfigClient m_LiveContentConfigClient;
        readonly IEnvironmentsApi m_EnvironmentsApi;
        readonly IDisplayDialog m_Dialog;
        readonly ILogger m_Logger;

        public DeleteRemoteCommand(
            ILiveContentConfigClient liveContentConfigClient,
            IEnvironmentsApi environmentsApi,
            IDisplayDialog dialog,
            ILogger logger)
        {
            m_LiveContentConfigClient = liveContentConfigClient;
            m_EnvironmentsApi = environmentsApi;
            m_Dialog = dialog;
            m_Logger = logger;
        }
        public override string Name { get; } = "Delete Remote";
        public override async Task ExecuteAsync(IEnumerable<CatalogEntryDeploymentItem> items, CancellationToken cancellationToken = default)
        {
            var itemList = items.ToList();
            await m_EnvironmentsApi.RefreshAsync();
            await m_LiveContentConfigClient.Initialize(m_EnvironmentsApi.ActiveEnvironmentId.ToString(), CloudProjectSettings.projectId, cancellationToken);
            var remoteItems = await m_LiveContentConfigClient.List(cancellationToken);
            var remoteHash = remoteItems.Select(i => i.CatalogListingId).ToHashSet();
            var toDeleteString = string.Join("\n", itemList.Select(i =>
            {
                if (remoteHash.Contains(i.CatalogItem.CatalogListingId))
                    return $"- {i.CatalogItem.CatalogListingId}";
                return $"- {i.CatalogItem.CatalogListingId} (will not be deleted, missing remotely)";
            }));

            var dialogResult = m_Dialog.Show(
                "Delete Catalog Items",
                $"Are you sure you want to delete the following Catalog Items from the remote catalog? \n {toDeleteString}",
                "Yes",
                "Cancel");

            if (!dialogResult)
            {
                return;
            }

            var itemsToDelete = itemList
                .Where(i => remoteHash.Contains(i.CatalogItem.CatalogListingId))
                .ToList();
            if (itemsToDelete.Any())
            {
                const int maxConcurrency = 8;
                using var gate = new SemaphoreSlim(maxConcurrency);
                await Task.WhenAll(itemsToDelete.Select(async item =>
                {
                    await gate.WaitAsync(cancellationToken);
                    try
                    {
                        await m_LiveContentConfigClient.Delete(item.CatalogItem, cancellationToken);
                        OnDeleteSuccess(item);
                    }
                    catch (OperationCanceledException) when(cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        OnDeleteFailure(item, e);
                        throw;
                    }
                    finally
                    {
                        gate.Release();
                    }
                }));
            }
        }

        void OnDeleteSuccess(CatalogEntryDeploymentItem item)
        {
            m_Logger.LogInfo($"Delete successful: {item.CatalogItem.CatalogListingId}");
            item.Status = DeploymentStatus.Empty;
        }

        void OnDeleteFailure(CatalogEntryDeploymentItem item, Exception exception)
        {
            m_Logger.LogError($"Delete failed: {item.CatalogItem.CatalogListingId}. Exception: {exception}");
        }
    }
}
