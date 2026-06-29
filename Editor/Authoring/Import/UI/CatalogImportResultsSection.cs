using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Editor.Authoring.Import.UI
{
    class CatalogImportResultsSection
    {
        const string k_HiddenClass = "hidden";

        VisualElement m_EntriesContainer;
        Toggle m_HeaderCaret;
        Label m_HeaderTag;
        Button m_RemoveAllBtn;
        CatalogEntryListView m_ListView;

        public CatalogImportResultsSection(VisualElement root, string prefix, string entryClass, bool readOnly, string iconName)
        {
            m_HeaderCaret = root.Q<Toggle>($"{prefix}HeaderCaret");
            m_HeaderTag = root.Q<Label>($"{prefix}HeaderTag");
            m_RemoveAllBtn = root.Q<Button>($"{prefix}RemoveAllBtn");
            m_EntriesContainer = root.Q<VisualElement>($"{prefix}EntriesContainer");

            m_ListView = new CatalogEntryListView(m_EntriesContainer, entryClass, readOnly);

            InsertIcon(root, $"{prefix}HeaderContainer", iconName);

            m_HeaderCaret.RegisterValueChangedCallback(evt =>
                m_EntriesContainer.EnableInClassList(k_HiddenClass, !evt.newValue));
        }

        public void WireRemoveAll(Action onRemoveAll)
        {
            if (m_RemoveAllBtn != null)
                m_RemoveAllBtn.clicked += onRemoveAll;
        }

        public void Rebuild(
            IReadOnlyList<ImportedCatalogEntry> entries,
            IReadOnlyDictionary<ImportedCatalogEntry, HashSet<string>> changedFields,
            IReadOnlyDictionary<string, CatalogItem> existingItems,
            Action<ImportedCatalogEntry> onDelete)
        {
            m_HeaderTag.text = $"({entries.Count})";

            if (m_RemoveAllBtn != null)
                m_RemoveAllBtn.EnableInClassList(k_HiddenClass, entries.Count == 0);

            m_ListView.Rebuild(entries, changedFields, existingItems, onDelete);
        }

        static void InsertIcon(VisualElement root, string containerName, string iconNameOrPath)
        {
            var container = root.Q<VisualElement>(containerName);
            if (container == null)
            {
                return;
            }

            var icon = new Image();
            icon.AddToClassList("pm-header-icon");
            var texture = (iconNameOrPath.StartsWith("Assets/") || iconNameOrPath.StartsWith("Packages/") ? AssetDatabase.LoadAssetAtPath<Texture2D>(iconNameOrPath) : null)
                ?? EditorGUIUtility.IconContent(iconNameOrPath)?.image;
            if (texture != null)
            {
                icon.image = texture;
            }

            container.Insert(1, icon);
        }
    }
}
