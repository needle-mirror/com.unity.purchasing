using System;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Editor.Authoring.Import.UI
{
    /// <summary>
    /// Manages the right panel of the Catalog Import window: the New, Modified, and Unmodified
    /// entry sections. Internally creates three <see cref="CatalogImportResultsSection"/> instances.
    /// </summary>
    class CatalogImportResultsPanelView
    {
        const string k_HiddenClass = "hidden";

        readonly VisualElement m_Root;
        readonly CatalogImportState m_State;

        Label m_EmptyHint;
        ScrollView m_RightScroll;

        CatalogImportResultsSection m_NewSection;
        CatalogImportResultsSection m_ModifiedSection;
        CatalogImportResultsSection m_UnmodifiedSection;

        public CatalogImportResultsPanelView(VisualElement root, CatalogImportState state)
        {
            m_Root = root ?? throw new ArgumentNullException(nameof(root));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>
        /// Queries elements, inserts section icons, wires callbacks, and subscribes to state events.
        /// Call this once after construction.
        /// </summary>
        public void Initialize()
        {
            m_EmptyHint = m_Root.Q<Label>("EmptyHint");
            m_RightScroll = m_Root.Q<ScrollView>("RightScroll");

            const string k_IconBasePath = "Packages/com.unity.purchasing/Editor/Authoring/Import/UI/Assets/";
            m_NewSection = new CatalogImportResultsSection(m_Root, "New", "entry-box--new", readOnly: false, k_IconBasePath + "icon-new.png");
            m_ModifiedSection = new CatalogImportResultsSection(m_Root, "Modified", "entry-box--modified", readOnly: false, k_IconBasePath + "icon-modified.png");
            m_UnmodifiedSection = new CatalogImportResultsSection(m_Root, "Unmodified", "entry-box--unmodified", readOnly: true, "DefaultAsset Icon");

            m_NewSection.WireRemoveAll(() =>
            {
                foreach (var e in m_State.NewEntries) m_State.FetchedEntries.Remove(e);
                m_State.NewEntries.Clear();
                m_State.NotifyChanged();
            });

            m_ModifiedSection.WireRemoveAll(() =>
            {
                foreach (var e in m_State.ModifiedEntries)
                {
                    m_State.FetchedEntries.Remove(e);
                    m_State.ChangedFields.Remove(e);
                }
                m_State.ModifiedEntries.Clear();
                m_State.NotifyChanged();
            });

            m_State.StateChanged += Rebuild;

            Rebuild();
        }

        internal void Rebuild()
        {
            var hasData = m_State.FetchedEntries.Count > 0;
            m_EmptyHint.EnableInClassList(k_HiddenClass, hasData);
            m_RightScroll.EnableInClassList(k_HiddenClass, !hasData);

            m_NewSection.Rebuild(m_State.NewEntries, null, null, OnDeleteEntry);
            m_ModifiedSection.Rebuild(m_State.ModifiedEntries, m_State.ChangedFields, m_State.ExistingCatalogItems, OnDeleteEntry);
            m_UnmodifiedSection.Rebuild(m_State.UnmodifiedEntries, null, null, null);
        }

        void OnDeleteEntry(ImportedCatalogEntry entry)
        {
            m_State.FetchedEntries.Remove(entry);
            m_State.NewEntries.Remove(entry);
            m_State.ModifiedEntries.Remove(entry);
            m_State.UnmodifiedEntries.Remove(entry);
            m_State.ChangedFields.Remove(entry);
            m_State.NotifyChanged();
        }
    }
}
