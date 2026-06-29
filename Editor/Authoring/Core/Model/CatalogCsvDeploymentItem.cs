using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Services.DeploymentApi.Editor;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    class CatalogCsvDeploymentItem : IDeploymentItem, ITypedItem
    {
        float m_Progress;
        DeploymentStatus m_Status;
        string m_Path;
        string m_Name;

        public CatalogCsvDeploymentItem(string path)
        {
            Name = System.IO.Path.GetFileName(path);
            Path = path;
        }

        public string Type => "Catalog CSV";

        public string Name
        {
            get => m_Name;
            set => SetField(ref m_Name, value);
        }

        public string Path
        {
            get => m_Path;
            set => SetField(ref m_Path, value);
        }

        public float Progress
        {
            get => m_Progress;
            set => SetField(ref m_Progress, value);
        }

        public DeploymentStatus Status
        {
            get => m_Status;
            set => SetField(ref m_Status, value);
        }

        public ObservableCollection<AssetState> States { get; } = new();

        public List<CatalogItem> CatalogItems { get; set; } = new();

        public List<CatalogEntryDeploymentItem> EntryDeploymentItems { get; set; } = new();

        public void Validate()
        {
            ClearTypedStates(CatalogItem.ValidationStateType);

            var entries = EntryDeploymentItems ?? new List<CatalogEntryDeploymentItem>();
            var errors = 0;
            var warnings = 0;
            var detailLines = new List<string>();
            foreach (var entry in entries)
            {
                entry.Validate(null);
                var entryStates = entry.States
                    .Where(s => s.Type == CatalogItem.ValidationStateType)
                    .ToList();
                if (entryStates.Count == 0)
                    continue;

                var worst = entryStates.Max(s => s.Level);
                if (worst == SeverityLevel.Error)
                    errors++;
                else if (worst == SeverityLevel.Warning)
                    warnings++;

                var id = EntryDisplayId(entry);
                foreach (var state in entryStates)
                    detailLines.Add($"{id}: {state.Description}");
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
                    return item.CatalogListingId;
                if (!string.IsNullOrEmpty(item.uSku))
                    return item.uSku;
            }
            return string.IsNullOrEmpty(entry.Name) ? "(unnamed)" : entry.Name;
        }

        internal void ClearTypedStates(string ownedType)
        {
            var i = 0;
            while (i < States.Count)
            {
                if (States[i].Type == ownedType)
                    States.RemoveAt(i);
                else
                    i++;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetField<T>(
            ref T field,
            T value,
            Action<T> onFieldChanged = null,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            onFieldChanged?.Invoke(field);
        }
    }
}
