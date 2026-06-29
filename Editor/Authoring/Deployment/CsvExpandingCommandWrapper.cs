using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    class CsvExpandingCommandWrapper : Command
    {
        readonly Command m_Inner;

        public override string Name => m_Inner.Name;

        public CsvExpandingCommandWrapper(Command inner)
        {
            m_Inner = inner;
        }

        public override async Task ExecuteAsync(
            IEnumerable<IDeploymentItem> items,
            CancellationToken cancellationToken = default)
        {
            var (entryItems, csvSources) = Expand(items);

            var handlers = BindCsvProgress(csvSources);
            var failed = false;
            try
            {
                await m_Inner.ExecuteAsync(entryItems, cancellationToken);
            }
            catch
            {
                failed = true;
                throw;
            }
            finally
            {
                UnbindCsvProgress(handlers);
                if (failed)
                {
                    MarkAllFailed(csvSources);
                }
                else
                {
                    PropagateStatus(csvSources);
                }
            }
        }

        static List<(CatalogEntryDeploymentItem entry, PropertyChangedEventHandler handler)> BindCsvProgress(
            List<(CatalogCsvDeploymentItem csv, List<CatalogEntryDeploymentItem> entries)> csvSources)
        {
            var handlers = new List<(CatalogEntryDeploymentItem, PropertyChangedEventHandler)>();

            foreach (var (csv, entries) in csvSources)
            {
                csv.Progress = 0f;
                csv.Status = new DeploymentStatus();

                foreach (var entry in entries)
                {
                    PropertyChangedEventHandler handler = (_, args) =>
                    {
                        if (args.PropertyName == nameof(IDeploymentItem.Progress))
                        {
                            var sum = 0f;
                            for (var i = 0; i < entries.Count; i++)
                            {
                                sum += entries[i].Progress;
                            }
                            csv.Progress = entries.Count > 0 ? sum / entries.Count : 0f;
                        }
                    };
                    entry.PropertyChanged += handler;
                    handlers.Add((entry, handler));
                }
            }

            return handlers;
        }

        static void UnbindCsvProgress(
            List<(CatalogEntryDeploymentItem entry, PropertyChangedEventHandler handler)> handlers)
        {
            foreach (var (entry, handler) in handlers)
            {
                entry.PropertyChanged -= handler;
            }
        }

        static void MarkAllFailed(
            List<(CatalogCsvDeploymentItem csv, List<CatalogEntryDeploymentItem> entries)> csvSources)
        {
            foreach (var (csv, _) in csvSources)
            {
                csv.Progress = 100f;
                csv.Status = DeploymentStatus.GetFailedToDeploy("Deployment failed");
            }
        }

        static void PropagateStatus(
            List<(CatalogCsvDeploymentItem csv, List<CatalogEntryDeploymentItem> entries)> csvSources)
        {
            foreach (var (csv, entries) in csvSources)
            {
                if (entries.Count == 0)
                {
                    csv.Progress = 100f;
                    csv.Status = new DeploymentStatus("No items to deploy", string.Empty, SeverityLevel.Warning);
                    continue;
                }

                var failedEntries = entries
                    .Where(e => e.Status.MessageSeverity == SeverityLevel.Error)
                    .ToList();

                csv.Progress = 100f;
                if (failedEntries.Count > 0)
                {
                    var perEntry = failedEntries
                        .Select(e => $"- {EntryDisplayId(e)}: {e.Status.MessageDetail}");
                    var detail =
                        $"{failedEntries.Count} of {entries.Count} item(s) failed\n" +
                        string.Join("\n", perEntry) + "\n";
                    csv.Status = Statuses.GetFailedToDeploy(detail);
                }
                else
                {
                    csv.Status = Statuses.GetDeployed($"{entries.Count} item(s) deployed");
                }
            }
        }

        static string EntryDisplayId(CatalogEntryDeploymentItem entry)
        {
            var item = entry.CatalogItem;
            if (item != null)
            {
                if (!string.IsNullOrEmpty(item.CatalogListingId))
                    return item.CatalogListingId;
                if (!string.IsNullOrEmpty(item.uSku))
                    return item.uSku;
            }
            return string.IsNullOrEmpty(entry.Name) ? "(unnamed)" : entry.Name;
        }

        static (List<IDeploymentItem> entryItems,
                List<(CatalogCsvDeploymentItem csv, List<CatalogEntryDeploymentItem> entries)> csvSources)
            Expand(IEnumerable<IDeploymentItem> items)
        {
            var entryItems = new List<IDeploymentItem>();
            var csvSources = new List<(CatalogCsvDeploymentItem, List<CatalogEntryDeploymentItem>)>();

            foreach (var item in items)
            {
                if (item is CatalogCsvDeploymentItem csv)
                {
                    var entries = csv.EntryDeploymentItems;
                    foreach (var entry in entries)
                    {
                        entryItems.Add(entry);
                    }
                    csvSources.Add((csv, entries));
                }
                else
                {
                    entryItems.Add(item);
                }
            }

            return (entryItems, csvSources);
        }
    }
}
