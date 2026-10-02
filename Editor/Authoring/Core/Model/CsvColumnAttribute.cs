using System;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    /// <summary>
    /// Marks a model member as a CSV column. Discovery is opt-in: <c>CatalogCsvParser</c> reflects
    /// over the annotated types and derives its header, its serializer and its reader from these
    /// attributes, so adding a column is one attribute on the model with no parser change.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    class CsvColumnAttribute : Attribute
    {
        /// <summary>Header text, matched case-insensitively; rows are never read by position.</summary>
        public string Name { get; }

        /// <summary>
        /// Position in the header, global across the model and stable — new columns append.
        /// </summary>
        public int Order { get; }

        /// <summary>Value assigned when the cell is blank, or when a <see cref="Converter"/> rejects it.</summary>
        public object Fallback { get; set; }

        /// <summary>
        /// Value meaning "not set": written as a blank cell, and not enough to create its object.
        /// </summary>
        public object Absent { get; set; }

        /// <summary>When false, a value equal to its type default is written as a blank cell.</summary>
        public bool EmitWhenDefault { get; set; } = true;

        /// <summary>Marks a column the parser reads and writes itself, outside the reflection path.</summary>
        public bool Identity { get; set; }

        /// <summary>
        /// Converter for a member whose CSV text doesn't follow the built-in conversion for its type.
        /// </summary>
        public Type Converter { get; set; }

        public CsvColumnAttribute(string name, int order)
        {
            Name = name;
            Order = order;
        }
    }
}
