using System.Collections.Generic;
using System.Linq;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Validations
{
    internal readonly struct OverrideConflictGroup
    {
        public readonly StoreId Store;
        public readonly string OverrideValue;
        public readonly IReadOnlyList<CatalogEntryDeploymentItem> Items;

        public OverrideConflictGroup(
            StoreId store,
            string overrideValue,
            IReadOnlyList<CatalogEntryDeploymentItem> items)
        {
            Store = store;
            OverrideValue = overrideValue;
            Items = items;
        }
    }

    static class StoreOverrideConflictValidation
    {
        internal static List<CatalogEntryDeploymentItem> FilterConflictingOverrides(
            IReadOnlyList<CatalogEntryDeploymentItem> resources,
            out IReadOnlyList<OverrideConflictGroup> conflictGroups)
        {
            var overrideEntries = resources
                .Where(r => r.CatalogItem?.StoreIdOverrides != null)
                .SelectMany(r => r.CatalogItem.StoreIdOverrides
                    .Where(o => o != null && !string.IsNullOrWhiteSpace(o.Value))
                    .Select(o => new { Store = o.Store, Value = o.Value, Item = r, USku = r.CatalogItem.uSku }))
                .ToList();

            conflictGroups = overrideEntries
                .GroupBy(e => (e.Store, e.Value))
                .Where(g => g.Select(e => e.USku).Distinct().Count() > 1)
                .Select(g => new OverrideConflictGroup(
                    g.Key.Store,
                    g.Key.Value,
                    g.Select(e => e.Item).Distinct().ToList()))
                .ToList();

            var conflictItems = new HashSet<CatalogEntryDeploymentItem>(
                conflictGroups.SelectMany(g => g.Items));

            return resources.Where(r => !conflictItems.Contains(r)).ToList();
        }

        public static void AddConflictStates(IReadOnlyList<CatalogEntryDeploymentItem> resources)
        {
            AddConflictStates(resources, stateTargets: null);
        }

        public static void AddConflictStates(
            IReadOnlyList<CatalogEntryDeploymentItem> submittedResources,
            IReadOnlyList<CatalogEntryDeploymentItem> remoteResources,
            ISet<CatalogEntryDeploymentItem> stateTargets)
        {
            var submittedIds = new HashSet<string>(
                submittedResources.Select(r => r.CatalogItem.CatalogListingId));

            var survivingRemote = remoteResources
                .Where(r => !submittedIds.Contains(r.CatalogItem.CatalogListingId));

            var effectiveCatalog = submittedResources
                .Concat(survivingRemote)
                .ToList();

            AddConflictStates(effectiveCatalog, stateTargets);
        }

        public static void AddConflictStates(
            IReadOnlyList<CatalogEntryDeploymentItem> effectiveCatalog,
            ISet<CatalogEntryDeploymentItem> stateTargets)
        {
            FilterConflictingOverrides(effectiveCatalog, out var conflictGroups);

            foreach (var group in conflictGroups)
            {
                foreach (var item in group.Items)
                {
                    if (stateTargets != null && !stateTargets.Contains(item))
                    {
                        continue;
                    }

                    var (shortMsg, longMsg) = GetConflictErrorMessages(item, group);
                    item.States.Add(new AssetState(
                        shortMsg,
                        longMsg,
                        SeverityLevel.Error,
                        CatalogItem.ValidationStateType));
                }
            }
        }

        public static (string shortMsg, string longMsg) GetConflictErrorMessages(
            CatalogEntryDeploymentItem target,
            OverrideConflictGroup conflictGroup)
        {
            var others = conflictGroup.Items
                .Where(item => item != target)
                .ToList();

            var otherLabels = string.Join(", ", others.Select(d => $"'{DisplayLabel(d)}'"));
            var targetLabel = DisplayLabel(target);
            var shortMsg = $"Store override conflict: '{targetLabel}' shares {conflictGroup.Store} override '{conflictGroup.OverrideValue}' with {otherLabels}";
            var longMsg = $"Multiple products with different SKUs use the same {conflictGroup.Store} store override '{conflictGroup.OverrideValue}'. "
                + "Each store-specific product ID must map to a single SKU. "
                + $"Conflicting items: '{targetLabel}', {otherLabels}";
            return (shortMsg, longMsg);
        }

        static string DisplayLabel(CatalogEntryDeploymentItem item)
        {
            if (item.Path == "Remote")
            {
                return $"Remote ({item.CatalogItem.CatalogListingId})";
            }
            return item.Path;
        }
    }
}
