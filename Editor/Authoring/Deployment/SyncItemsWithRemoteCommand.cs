using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.Batching;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using UnityEngine;

using ILogger = UnityEditor.Purchasing.Editor.Authoring.Core.Logger.ILogger;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    class SyncItemsWithRemoteCommand : Command
    {
        /// <summary>
        /// When the number of items to sync is at or below this threshold,
        /// individual GET requests are used instead of fetching the full catalog.
        /// Matches the batch size used by <see cref="Batching"/> for rate limiting.
        /// </summary>
        const int k_IndividualFetchLimit = 10;

        readonly ILiveContentConfigClient m_Client;
        readonly IEnvironmentsApi m_EnvironmentsApi;
        readonly ILogger m_Logger;

        public SyncItemsWithRemoteCommand(
            ILiveContentConfigClient client,
            IEnvironmentsApi environmentsApi,
            ILogger logger)
        {
            m_Client = client;
            m_EnvironmentsApi = environmentsApi;
            m_Logger = logger;
        }

        public override string Name => L10n.Tr("Sync with Remote");

        public override async Task ExecuteAsync(
            IEnumerable<IDeploymentItem> items,
            CancellationToken cancellationToken = default)
        {
            var itemList = items.ToList();
            var entryItems = itemList.OfType<CatalogEntryDeploymentItem>().ToList();
            var csvItems = itemList.OfType<CatalogCsvDeploymentItem>().ToList();
            var allEntries = entryItems
                .Concat(csvItems.SelectMany(csv => csv.EntryDeploymentItems))
                .ToList();

            MarkChecking(entryItems, csvItems);

            try
            {
                await m_Client.Initialize(
                    m_EnvironmentsApi.ActiveEnvironmentId.ToString(),
                    CloudProjectSettings.projectId,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                m_Logger.LogError(e);
                MarkFetchFailed(allEntries, csvItems, e.Message);
                return;
            }

            await SyncEntries(allEntries, cancellationToken);

            foreach (var csv in csvItems)
            {
                AggregateCsvStatus(csv);
            }
        }

        async Task SyncEntries(
            List<CatalogEntryDeploymentItem> entries,
            CancellationToken ct)
        {
            if (entries.Count <= k_IndividualFetchLimit)
                await SyncIndividually(entries, ct);
            else
                await SyncViaFullCatalog(entries, ct);
        }

        async Task SyncIndividually(
            List<CatalogEntryDeploymentItem> entries,
            CancellationToken ct)
        {
            await Batching.ExecuteInBatchesAsync(
                entries,
                entry => SyncEntry(entry, ct),
                ct);
        }

        async Task SyncEntry(CatalogEntryDeploymentItem entry, CancellationToken ct)
        {
            var id = entry.CatalogItem?.CatalogListingId;
            if (string.IsNullOrEmpty(id))
            {
                entry.Status = Statuses.GetAhead("Not yet assigned a catalog listing ID");
                entry.Progress = 100f;
                return;
            }

            try
            {
                var remote = await m_Client.Get(id, ct);
                entry.Status = DetermineStatus(entry.CatalogItem, remote);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                entry.Status = Statuses.GetFailedToFetch(e.Message);
            }

            entry.Progress = 100f;
        }

        async Task SyncViaFullCatalog(
            List<CatalogEntryDeploymentItem> entries,
            CancellationToken ct)
        {
            List<CatalogItem> remoteList;
            try
            {
                remoteList = await m_Client.List(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                m_Logger.LogError(e);
                MarkFetchFailed(entries, new List<CatalogCsvDeploymentItem>(), e.Message);
                return;
            }

            var remoteMap = remoteList.ToDictionary(ci => ci.CatalogListingId);
            foreach (var entry in entries)
            {
                ApplySyncStatus(entry, remoteMap);
            }
        }

        static DeploymentStatus DetermineStatus(CatalogItem local, CatalogItem remote)
        {
            if (remote == null)
                return Statuses.GetNotDeployed();

            return local.ContentEquals(remote)
                ? Statuses.GetUpToDate()
                : Statuses.GetAhead("Local changes not yet deployed");
        }

        static void MarkChecking(
            List<CatalogEntryDeploymentItem> entryItems,
            List<CatalogCsvDeploymentItem> csvItems)
        {
            foreach (var entry in entryItems)
            {
                entry.Status = Statuses.Checking;
                entry.Progress = 0f;
            }

            foreach (var csv in csvItems)
            {
                csv.Status = Statuses.Checking;
                csv.Progress = 0f;
            }
        }

        static void ApplySyncStatus(
            CatalogEntryDeploymentItem entry,
            Dictionary<string, CatalogItem> remoteMap)
        {
            var id = entry.CatalogItem?.CatalogListingId;

            if (string.IsNullOrEmpty(id))
            {
                entry.Status = Statuses.GetAhead("Not yet assigned a catalog listing ID");
                entry.Progress = 100f;
                return;
            }

            if (!remoteMap.TryGetValue(id, out var remoteItem))
            {
                entry.Status = Statuses.GetNotDeployed();
                entry.Progress = 100f;
                return;
            }

            entry.Status = entry.CatalogItem.ContentEquals(remoteItem)
                ? Statuses.GetUpToDate()
                : Statuses.GetAhead("Local changes not yet deployed");

            entry.Progress = 100f;
        }

        static void AggregateCsvStatus(CatalogCsvDeploymentItem csv)
        {
            var entries = csv.EntryDeploymentItems;

            if (entries.Count == 0)
            {
                csv.Status = Statuses.GetUpToDate();
                csv.Progress = 100f;
                return;
            }

            var failedEntries = entries
                .Where(e => e.Status.MessageSeverity == SeverityLevel.Error)
                .ToList();

            var needsDeployingEntries = entries
                .Where(e
                    => e.Status.Message == Statuses.k_Ahead
                    || e.Status.Message == Statuses.k_NotDeployed)
                .ToList();

            if (failedEntries.Count > 0)
            {
                csv.Status = Statuses.GetFailedToFetch(
                    $"{failedEntries.Count} item(s) failed to sync");
            }
            else if (needsDeployingEntries.Count > 0)
            {
                var ids = string.Join(", ", needsDeployingEntries.Select(
                    e => e.CatalogItem?.CatalogListingId ?? e.Name));
                csv.Status = Statuses.GetAhead(
                    $"{needsDeployingEntries.Count} item(s) need deploying: {ids}");
            }
            else
            {
                csv.Status = Statuses.GetUpToDate();
            }

            csv.Progress = 100f;
        }

        static void MarkFetchFailed(
            List<CatalogEntryDeploymentItem> allEntries,
            List<CatalogCsvDeploymentItem> csvItems,
            string message)
        {
            foreach (var entry in allEntries)
            {
                entry.Status = Statuses.GetFailedToFetch(message);
                entry.Progress = 100f;
            }

            foreach (var csv in csvItems)
            {
                csv.Status = Statuses.GetFailedToFetch(message);
                csv.Progress = 100f;
            }
        }
    }
}
