using System.Collections.Generic;
using System.Linq;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Validations;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    class CatalogCsvDeploymentItem : DeploymentItemBase
    {
        public CatalogCsvDeploymentItem(string path) : base(path) { }

        public override string Type => "Catalog CSV";

        public List<CatalogItem> CatalogItems { get; set; } = new();

        public List<CatalogEntryDeploymentItem> EntryDeploymentItems { get; set; } = new();

        public void Validate()
        {
            ClearTypedStates(CatalogItem.ValidationStateType);

            var entries = EntryDeploymentItems ?? new List<CatalogEntryDeploymentItem>();

            foreach (var entry in entries)
            {
                entry.Validate(null);
            }

            StoreOverrideConflictValidation.AddConflictStates(entries);

            var errors = 0;
            var warnings = 0;
            var detailLines = new List<string>();
            foreach (var entry in entries)
            {
                var entryStates = entry.States
                    .Where(s => s.Type == CatalogItem.ValidationStateType)
                    .ToList();
                if (entryStates.Count == 0)
                {
                    continue;
                }

                var worst = entryStates.Max(s => s.Level);
                if (worst == SeverityLevel.Error)
                {
                    errors++;
                }
                else if (worst == SeverityLevel.Warning)
                {
                    warnings++;
                }

                var id = EntryDisplayId(entry);
                foreach (var state in entryStates)
                {
                    detailLines.Add($"{id}: {state.Description}");
                }
            }

            if (errors > 0)
            {
                States.Add(new AssetState(
                    $"{errors} of {entries.Count} item(s) have validation errors",
                    "- " + string.Join("\n- ", detailLines) + "\n",
                    SeverityLevel.Error,
                    CatalogItem.ValidationStateType));
            }
            else if (warnings > 0)
            {
                States.Add(new AssetState(
                    $"{warnings} of {entries.Count} item(s) have validation warnings",
                    "- " + string.Join("\n- ", detailLines) + "\n",
                    SeverityLevel.Warning,
                    CatalogItem.ValidationStateType));
            }
        }

        static string EntryDisplayId(CatalogEntryDeploymentItem entry)
        {
            var item = entry.CatalogItem;
            if (item != null)
            {
                if (!string.IsNullOrEmpty(item.CatalogListingId))
                {
                    return item.CatalogListingId;
                }
                if (!string.IsNullOrEmpty(item.uSku))
                {
                    return item.uSku;
                }
            }
            return string.IsNullOrEmpty(entry.Name) ? "(unnamed)" : entry.Name;
        }
    }
}
