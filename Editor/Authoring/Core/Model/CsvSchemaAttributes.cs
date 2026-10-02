using System;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    /// <summary>
    /// Marks a collection on CatalogItem that the CSV pivots into rows: one entry per row, gathered back
    /// into the list by <see cref="KeyMember"/> when reading. The element type's own
    /// <see cref="CsvColumnAttribute"/> members become the columns, so ProductDetails contributes Title,
    /// Description and so on. A <c>List&lt;string&gt;</c> has no members to annotate, so it carries a
    /// <see cref="CsvColumnAttribute"/> itself and the cell is the whole entry.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    class CsvRowCollectionAttribute : Attribute
    {
        /// <summary>
        /// Member of the element type identifying an entry; null keys entries on the cell itself.
        /// </summary>
        public string KeyMember { get; set; }

        /// <summary>
        /// Members whose cells must be usable for the row to contribute an entry; null requires the key alone.
        /// </summary>
        public string[] RequiredMembers { get; set; }

        /// <summary>Whether a repeated key is reported rather than deduped in silence.</summary>
        public bool ReportDuplicates { get; set; } = true;

        /// <summary>Whether a list that collected no entries is left null rather than empty.</summary>
        public bool NullWhenEmpty { get; set; }

        /// <summary>Whether two keys differing only in case are one entry.</summary>
        public bool IgnoreKeyCase { get; set; }
    }

    /// <summary>
    /// Marks a single nested object whose members flatten into the owning row — Promotion on the item,
    /// Badge on a product detail. The object is built only when <see cref="RequiredMember"/> has a usable
    /// cell, and stays null otherwise, so blank promotion columns don't leave an empty Promotion behind.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    class CsvNestedAttribute : Attribute
    {
        /// <summary>Member of the nested type that decides whether the object exists at all.</summary>
        public string RequiredMember { get; set; }
    }
}
