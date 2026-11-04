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
    /// <summary>
    /// Compares local catalog items against the remote and reports only discrepancies.
    /// Sync is triggered by local actions, so a positive "Up to date" would go stale as soon
    /// as the remote changed; items that match the remote keep a prior "Deployed" status or
    /// are cleared instead.
    /// </summary>
    class SyncItemsWithRemoteCommand : Command
    {
        /// <summary>
        /// When the number of items to sync is at or below this threshold,
        /// individual GET requests are used instead of fetching the full catalog.
        /// Matches the batch size used by <see cref="Batching"/> for rate limiting.
        /// </summary>
        const int k_IndividualFetchLimit = 10;

        const string k_ProjectNotLinked = "The project is not linked to a Unity Cloud project.";
        const string k_NoEnvironment = "No Unity Cloud environment is selected.";

        readonly ILiveContentConfigClient m_Client;
        readonly IEnvironmentsApi m_EnvironmentsApi;
        readonly ICloudProjectLink m_ProjectLink;
        readonly ILogger m_Logger;
        string m_LastSyncedEnvironmentId;

        public SyncItemsWithRemoteCommand(
            ILiveContentConfigClient client,
            IEnvironmentsApi environmentsApi,
            ICloudProjectLink projectLink,
            ILogger logger)
        {
            m_Client = client;
            m_EnvironmentsApi = environmentsApi;
            m_ProjectLink = projectLink;
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

            var snapshot = DeployedStatusSnapshot.Capture(allEntries.Concat<IDeploymentItem>(csvItems));
            MarkChecking(entryItems, csvItems);

            var environmentId = await TryInitializeClient(allEntries, csvItems, cancellationToken);
            if (environmentId == null)
                return;

            ForgetDeployResultsFromOtherEnvironment(snapshot, environmentId);
            await SyncEntries(allEntries, snapshot, cancellationToken);

            foreach (var csv in csvItems)
            {
                AggregateCsvStatus(csv, snapshot);
            }
        }

        /// <summary>
        /// Returns the environment id the client was initialized against, or null after marking
        /// every item as failed.
        /// </summary>
        async Task<string> TryInitializeClient(
            List<CatalogEntryDeploymentItem> allEntries,
            List<CatalogCsvDeploymentItem> csvItems,
            CancellationToken ct)
        {
            try
            {
                var target = await ResolveRemoteTargetAsync(ct);
                await m_Client.Initialize(target.EnvironmentId, target.ProjectId, ct);
                return target.EnvironmentId;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (RemoteTargetUnavailableException e)
            {
                MarkFetchFailed(allEntries, csvItems, e.Message);
                return null;
            }
            catch (Exception e)
            {
                m_Logger.LogError(e);
                MarkFetchFailed(allEntries, csvItems, e.Message);
                return null;
            }
        }

        /// <summary>
        /// A "Deployed" result belongs to the environment it was deployed to. Deployment package
        /// versions up to 1.7.2 don't clear statuses when the active environment changes, so a
        /// result from the previous environment must not be restored against the new one.
        /// </summary>
        void ForgetDeployResultsFromOtherEnvironment(DeployedStatusSnapshot snapshot, string environmentId)
        {
            if (m_LastSyncedEnvironmentId != null && m_LastSyncedEnvironmentId != environmentId)
                snapshot.Clear();

            m_LastSyncedEnvironmentId = environmentId;
        }

        async Task<(string EnvironmentId, string ProjectId)> ResolveRemoteTargetAsync(CancellationToken ct)
        {
            var projectId = await m_ProjectLink.WaitForProjectIdAsync(ct);
            if (string.IsNullOrEmpty(projectId))
                throw new RemoteTargetUnavailableException(k_ProjectNotLinked);

            var environmentId = await GetActiveEnvironmentIdAsync();
            if (environmentId == Guid.Empty)
                throw new RemoteTargetUnavailableException(k_NoEnvironment);

            return (environmentId.ToString(), projectId);
        }

        /// <summary>
        /// On editor startup the active environment can still be null or <see cref="Guid.Empty"/>,
        /// whose all-zero string passes client validation and gets a 404 that reads as "Not Deployed".
        /// Returns <see cref="Guid.Empty"/> if no environment is available after a refresh.
        /// </summary>
        async Task<Guid> GetActiveEnvironmentIdAsync()
        {
            if (!HasActiveEnvironment())
                await m_EnvironmentsApi.RefreshAsync();

            return m_EnvironmentsApi.ActiveEnvironmentId ?? Guid.Empty;
        }

        bool HasActiveEnvironment()
            => (m_EnvironmentsApi.ActiveEnvironmentId ?? Guid.Empty) != Guid.Empty;

        async Task SyncEntries(
            List<CatalogEntryDeploymentItem> entries,
            DeployedStatusSnapshot snapshot,
            CancellationToken ct)
        {
            if (entries.Count <= k_IndividualFetchLimit)
                await SyncIndividually(entries, snapshot, ct);
            else
                await SyncViaFullCatalog(entries, snapshot, ct);
        }

        async Task SyncIndividually(
            List<CatalogEntryDeploymentItem> entries,
            DeployedStatusSnapshot snapshot,
            CancellationToken ct)
        {
            await Batching.ExecuteInBatchesAsync(
                entries,
                entry => SyncEntry(entry, snapshot, ct),
                ct);
        }

        async Task SyncEntry(
            CatalogEntryDeploymentItem entry,
            DeployedStatusSnapshot snapshot,
            CancellationToken ct)
        {
            if (MarkAheadIfUnassigned(entry))
                return;

            try
            {
                var remote = await m_Client.Get(entry.CatalogItem.CatalogListingId, ct);
                entry.Status = DetermineStatus(entry, remote, snapshot);
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
            DeployedStatusSnapshot snapshot,
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
                ApplySyncStatus(entry, remoteMap, snapshot);
            }
        }

        static void ApplySyncStatus(
            CatalogEntryDeploymentItem entry,
            Dictionary<string, CatalogItem> remoteMap,
            DeployedStatusSnapshot snapshot)
        {
            if (MarkAheadIfUnassigned(entry))
                return;

            remoteMap.TryGetValue(entry.CatalogItem.CatalogListingId, out var remote);
            entry.Status = DetermineStatus(entry, remote, snapshot);
            entry.Progress = 100f;
        }

        static bool MarkAheadIfUnassigned(CatalogEntryDeploymentItem entry)
        {
            if (!string.IsNullOrEmpty(entry.CatalogItem?.CatalogListingId))
                return false;

            entry.Status = Statuses.GetAhead("Not yet assigned a catalog listing ID");
            entry.Progress = 100f;
            return true;
        }

        static DeploymentStatus DetermineStatus(
            CatalogEntryDeploymentItem entry,
            CatalogItem remote,
            DeployedStatusSnapshot snapshot)
        {
            if (remote == null)
                return Statuses.GetNotDeployed();

            return entry.CatalogItem.ContentEquals(remote)
                ? snapshot.StatusWhenInSync(entry)
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

        static void AggregateCsvStatus(CatalogCsvDeploymentItem csv, DeployedStatusSnapshot snapshot)
        {
            csv.Status = DetermineCsvStatus(csv, snapshot);
            csv.Progress = 100f;
        }

        static DeploymentStatus DetermineCsvStatus(CatalogCsvDeploymentItem csv, DeployedStatusSnapshot snapshot)
        {
            var entries = csv.EntryDeploymentItems;

            // An emptied CSV keeps its item (and its last status) across re-parses, and with no
            // entries nothing below would contradict a stale "Deployed".
            if (entries.Count == 0)
                return Statuses.NoItemsToDeploy;

            var failedCount = entries.Count(e => e.Status.MessageSeverity == SeverityLevel.Error);
            if (failedCount > 0)
                return Statuses.GetFailedToFetch($"{failedCount} item(s) failed to sync");

            var needsDeploying = entries.Where(NeedsDeploying).ToList();
            if (needsDeploying.Count > 0)
                return Statuses.GetAhead(DescribeNeedsDeploying(needsDeploying));

            return snapshot.StatusWhenInSync(csv);
        }

        static bool NeedsDeploying(CatalogEntryDeploymentItem entry)
            => entry.Status.Message == Statuses.k_Ahead
            || entry.Status.Message == Statuses.k_NotDeployed;

        static string DescribeNeedsDeploying(List<CatalogEntryDeploymentItem> entries)
        {
            var ids = string.Join(", ", entries.Select(
                e => e.CatalogItem?.CatalogListingId ?? e.Name));
            return $"{entries.Count} item(s) need deploying: {ids}";
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

        /// <summary>
        /// An expected, user-fixable reason the remote can't be reached yet (unlinked project, no
        /// environment). Reported on the items without logging an error.
        /// </summary>
        sealed class RemoteTargetUnavailableException : Exception
        {
            public RemoteTargetUnavailableException(string message)
                : base(message) { }
        }

        /// <summary>
        /// Remembers which items carried a "Deployed" status before the sync overwrote it with
        /// "Checking", so a user's deploy result survives a sync that finds nothing wrong.
        /// Any other prior status (e.g. a stale "Ahead") is deliberately dropped.
        /// </summary>
        sealed class DeployedStatusSnapshot
        {
            readonly Dictionary<IDeploymentItem, DeploymentStatus> m_Deployed = new();

            public static DeployedStatusSnapshot Capture(IEnumerable<IDeploymentItem> items)
            {
                var snapshot = new DeployedStatusSnapshot();
                foreach (var item in items.Where(i => Statuses.IsDeployed(i.Status)))
                {
                    snapshot.m_Deployed[item] = item.Status;
                }

                return snapshot;
            }

            public void Clear() => m_Deployed.Clear();

            public DeploymentStatus StatusWhenInSync(IDeploymentItem item)
                => m_Deployed.TryGetValue(item, out var status) ? status : DeploymentStatus.Empty;
        }
    }
}
