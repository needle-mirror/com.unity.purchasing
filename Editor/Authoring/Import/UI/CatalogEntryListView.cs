using System;
using System.Collections.Generic;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Editor.Authoring.Import.UI
{
    /// <summary>
    /// Renders a list of <see cref="ImportedCatalogEntry"/> objects into a given container element.
    /// This class has no dependency on <see cref="CatalogImportState"/> or
    /// <see cref="CatalogImportController"/> — it accepts only raw data.
    /// </summary>
    class CatalogEntryListView
    {
        readonly VisualElement m_Container;
        readonly string m_OutlineClass;
        readonly bool m_ReadOnly;

        public CatalogEntryListView(VisualElement container, string outlineClass, bool readOnly)
        {
            m_Container = container ?? throw new ArgumentNullException(nameof(container));
            m_OutlineClass = outlineClass;
            m_ReadOnly = readOnly;
        }

        /// <summary>
        /// Clears and rebuilds the container with the supplied entries.
        /// </summary>
        internal void Rebuild(
            IReadOnlyList<ImportedCatalogEntry> entries,
            IReadOnlyDictionary<ImportedCatalogEntry, HashSet<string>> changedFields,
            IReadOnlyDictionary<string, CatalogItem> existingItems,
            Action<ImportedCatalogEntry> onDelete)
        {
            m_Container.Clear();

            foreach (var entry in entries)
            {
                HashSet<string> changed = null;
                changedFields?.TryGetValue(entry, out changed);

                CatalogItem existingItem = null;
                existingItems?.TryGetValue(entry.Sku, out existingItem);

                var box = BuildEntryBox(entry, m_OutlineClass, changed, existingItem, m_ReadOnly, () => onDelete?.Invoke(entry));
                m_Container.Add(box);
            }
        }

        static VisualElement BuildEntryBox(
            ImportedCatalogEntry entry,
            string outlineClass,
            HashSet<string> changedFields,
            CatalogItem existingItem,
            bool readOnly,
            Action onDelete)
        {
            var foldout = new Foldout { text = entry.Sku, value = false };
            foldout.AddToClassList("entry-foldout");
            foldout.AddToClassList(outlineClass);

            if (!readOnly)
            {
                var toggle = foldout.Q<Toggle>();
                if (toggle != null)
                {
                    var deleteBtn = new Button(onDelete) { text = "\u2715" };
                    deleteBtn.AddToClassList("delete-btn");
                    toggle.Add(deleteBtn);
                }
            }

            string existingTitle = null;
            string existingDescription = null;
            string existingPrice = null;

            if (existingItem != null && changedFields != null)
            {
                if (existingItem.ProductDetails != null && existingItem.ProductDetails.Count > 0)
                {
                    existingTitle = existingItem.ProductDetails[0].Title;
                    existingDescription = existingItem.ProductDetails[0].Description;
                }
                if (existingItem.PricingDetails != null && existingItem.PricingDetails.Count > 0)
                {
                    var pd = existingItem.PricingDetails[0];
                    existingPrice = $"{pd.Amount:0.##} {pd.CurrencyCode ?? ""}".Trim();
                }
            }

            foldout.Add(BuildFieldRow("Title", entry.Title, changedFields, nameof(ImportedCatalogEntry.Title), existingTitle));
            foldout.Add(BuildFieldRow("Description", entry.Description, changedFields, nameof(ImportedCatalogEntry.Description), existingDescription));
            foldout.Add(BuildFieldRow("Price", $"{entry.Price:0.##} {entry.CurrencyCode}", changedFields, "Price", existingPrice));

            return foldout;
        }

        static VisualElement BuildFieldRow(
            string label,
            string value,
            HashSet<string> changedFields,
            string fieldName,
            string existingValue = null)
        {
            var labelElem = new Label($"{label}: {value ?? string.Empty}");
            labelElem.AddToClassList("field-row");

            var isChanged = changedFields != null && changedFields.Contains(fieldName);
            if (isChanged)
            {
                labelElem.AddToClassList("field-row--changed");
            }

            return labelElem;
        }
    }
}
