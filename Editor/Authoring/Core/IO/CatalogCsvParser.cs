using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.IO
{
    /// <summary>
    /// Reads and writes .catalog.csv. Which columns exist, what they convert to and where they sit on the
    /// model all come from <see cref="CatalogCsvSchema"/>, so what remains here is only what the CSV shape
    /// itself imposes: how rows gather into items, and what to say when two rows of one item disagree.
    /// </summary>
    class CatalogCsvParser : ICatalogCsvParser
    {
        public const string ParseStateType = "CatalogCsvParseIssue";

        static readonly char[] k_NeedsQuoting = { ',', '"', '\n', '\r' };

        static readonly CatalogCsvSchema s_Schema = CatalogCsvSchema.Catalog;
        static readonly CsvColumn s_Sku = s_Schema.Identity(nameof(CatalogItem.uSku));
        static readonly CsvColumn s_ListingId = s_Schema.Identity(nameof(CatalogItem.CatalogListingId));

        public List<CatalogItem> Parse(string csvContent, out List<AssetState> issues)
        {
            issues = new List<AssetState>();

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                return new List<CatalogItem>();
            }

            var lines = ParseLines(csvContent);
            if (lines.Count < 2)
            {
                return new List<CatalogItem>();
            }

            var row = new CsvRow(BuildColumnMap(lines[0]), issues);
            var idOrder = new List<string>();
            var items = new Dictionary<string, CatalogItem>(StringComparer.OrdinalIgnoreCase);
            var conflicts = new List<string>();
            var duplicates = new List<string>();
            var firstRowFor = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 1; i < lines.Count; i++)
            {
                var fields = lines[i];
                if (fields.Length == 0 || (fields.Length == 1 && string.IsNullOrWhiteSpace(fields[0])))
                {
                    continue;
                }

                row.Begin(fields, i + 1);

                var sku = row.Raw(s_Sku);
                if (string.IsNullOrWhiteSpace(sku))
                {
                    issues.Add(new AssetState(
                        $"Row {row.Number} skipped: missing Sku",
                        "Each data row must have a non-empty Sku.",
                        SeverityLevel.Warning, ParseStateType));
                    continue;
                }

                var declaredId = row.Raw(s_ListingId);
                var catalogListingId = string.IsNullOrWhiteSpace(declaredId)
                    ? CatalogItem.CatalogListingIdPrefix + sku
                    : declaredId;

                if (!items.TryGetValue(catalogListingId, out var item))
                {
                    item = new CatalogItem { CatalogListingId = catalogListingId, uSku = sku };
                    foreach (var group in s_Schema.RowGroups)
                    {
                        // Eagerly created so collecting an entry never has to null-check; the ones that
                        // prefer null to empty are cleared again once every row has been seen.
                        group.CreateList(item);
                    }

                    Apply(item, s_Schema.Item.Target, row);

                    items[catalogListingId] = item;
                    idOrder.Add(catalogListingId);
                    firstRowFor[catalogListingId] = row.Number;
                }
                else
                {
                    ReportItemConflicts(item, row, catalogListingId, firstRowFor[catalogListingId], conflicts);
                }

                foreach (var group in s_Schema.RowGroups)
                {
                    CollectEntry(item, group, row, catalogListingId, firstRowFor, duplicates, conflicts);
                }
            }

            var result = new List<CatalogItem>(idOrder.Count);
            foreach (var id in idOrder)
            {
                NullOutEmptyCollections(items[id]);
                result.Add(items[id]);
            }

            AddAggregate(issues, conflicts, "Row Conflicts (first occurrence kept)", SeverityLevel.Warning);
            AddAggregate(issues, duplicates, "Duplicate Rows (safely ignored)", SeverityLevel.Info);

            return result;
        }

        public string Serialize(List<CatalogItem> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine(s_Schema.Header);

            var lists = new IList[s_Schema.RowGroups.Length];

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.uSku))
                {
                    continue;
                }

                // One row per entry of the longest collection: an item with three languages and one
                // price spreads over three rows, its item-level cells repeated on each.
                var rowCount = 1;
                for (var g = 0; g < lists.Length; g++)
                {
                    lists[g] = s_Schema.RowGroups[g].ReadList(item);
                    rowCount = Math.Max(rowCount, lists[g]?.Count ?? 0);
                }

                for (var i = 0; i < rowCount; i++)
                {
                    for (var c = 0; c < s_Schema.Columns.Length; c++)
                    {
                        if (c > 0)
                        {
                            sb.Append(',');
                        }

                        sb.Append(CsvEscape(Cell(item, s_Schema.Columns[c], lists, i)));
                    }

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        static string Cell(CatalogItem item, CsvColumn column, IList[] lists, int rowIndex)
        {
            if (column.Identity)
            {
                // An item that never set a listing id writes its bare Sku, which the admin API then
                // rejects for missing the 'catalog/' prefix — by design, so the author sees it.
                return column == s_ListingId
                    ? (string.IsNullOrWhiteSpace(item.CatalogListingId) ? item.uSku : item.CatalogListingId)
                    : item.uSku;
            }

            object value;
            if (column.Group.IsItem)
            {
                value = column.Read(item);
            }
            else
            {
                var list = lists[column.Group.Index];
                if (list == null || rowIndex >= list.Count)
                {
                    // This row is past the end of the column's collection, so there's no entry to read.
                    // What the column declared as its fallback still stands: that's why Language spells
                    // out en-US on a row carrying nothing but a category.
                    return column.DeclaredFallback == null
                        ? string.Empty
                        : column.Converter.ToCsv(column.DeclaredFallback);
                }

                var entry = list[rowIndex];
                value = column.Group.ScalarElement ? entry : column.Read(entry);
            }

            if (value == null || column.IsAbsent(value))
            {
                return string.Empty;
            }

            if (!column.EmitWhenDefault && value.Equals(column.TypeDefault))
            {
                return string.Empty;
            }

            return column.Converter.ToCsv(value);
        }

        /// <summary>Fills an object from the row, creating nested objects only where required cells exist.</summary>
        static void Apply(object target, CsvTarget shape, CsvRow row)
        {
            foreach (var column in shape.Columns)
            {
                if (column.Identity)
                {
                    continue;
                }

                column.Member.SetValue(target, row.Value(column));
            }

            foreach (var nested in shape.Nested)
            {
                if (!row.TryRequired(nested.RequiredColumn, out _))
                {
                    continue;
                }

                var instance = nested.Create();
                nested.Write(target, instance);
                Apply(instance, nested.Target, row);
            }
        }

        /// <summary>The first row of an item wins; a later row whose item-level cells disagree is reported.</summary>
        static void ReportItemConflicts(CatalogItem item, CsvRow row, string context,
            int firstRow, List<string> conflicts)
        {
            foreach (var column in s_Schema.ItemConflictColumns)
            {
                if (string.IsNullOrWhiteSpace(row.Raw(column)))
                {
                    // Leaving an item-level cell blank on a continuation row isn't disagreeing — it's
                    // how most authors write them.
                    continue;
                }

                var incoming = row.Value(column);
                if (incoming == null)
                {
                    continue;
                }

                if (!incoming.Equals(column.Read(item)))
                {
                    AddMessage(conflicts, row.Number, context, column.Name, "conflicts with", firstRow);
                }
            }
        }

        /// <summary>Takes the row's entry for one collection, keyed by the group's key column.</summary>
        static void CollectEntry(CatalogItem item, CsvGroup group, CsvRow row, string context,
            Dictionary<string, int> firstRowFor, List<string> duplicates, List<string> conflicts)
        {
            // Converted before the row has earned an entry, so an unrecognized Language is reported
            // whether or not this row goes on to contribute a product detail.
            var key = row.Value(group.KeyColumn);

            foreach (var required in group.RequiredColumns)
            {
                if (!row.TryRequired(required, out _))
                {
                    return;
                }
            }

            var list = group.ReadList(item) ?? group.CreateList(item);
            var keyText = key?.ToString() ?? string.Empty;
            var seenAt = $"{context}|{group.Name}|{keyText}";
            var existing = FindEntry(list, group, key);

            if (existing == null)
            {
                firstRowFor[seenAt] = row.Number;
                list.Add(group.ScalarElement ? key : BuildEntry(group, row));
                return;
            }

            if (!group.ReportDuplicates)
            {
                return;
            }

            CheckDuplicate(IsIdentical(existing, group, row), group.Name, $"{context}, {keyText}",
                row.Number, firstRowFor[seenAt], duplicates, conflicts);
        }

        static object BuildEntry(CsvGroup group, CsvRow row)
        {
            var entry = group.CreateElement();
            Apply(entry, group.Target, row);
            return entry;
        }

        static object FindEntry(IList list, CsvGroup group, object key)
        {
            foreach (var entry in list)
            {
                var candidate = group.ScalarElement ? entry : group.KeyColumn.Read(entry);
                if (KeyMatches(group, candidate, key))
                {
                    return entry;
                }
            }

            return null;
        }

        static bool KeyMatches(CsvGroup group, object candidate, object key)
        {
            if (group.IgnoreKeyCase && candidate is string text && key is string other)
            {
                return string.Equals(text, other, StringComparison.OrdinalIgnoreCase);
            }

            return Equals(candidate, key);
        }

        /// <summary>Whether a repeated key says the same thing as the entry already collected.</summary>
        static bool IsIdentical(object existing, CsvGroup group, CsvRow row)
        {
            foreach (var column in group.ComparedColumns)
            {
                // A cell the row wouldn't build holds nothing, the same as an absent nested object.
                var incoming = RowBuildsColumn(column, row) ? row.Value(column) : null;
                if (!Equals(incoming, column.Read(existing)))
                {
                    return false;
                }
            }

            return true;
        }

        static bool RowBuildsColumn(CsvColumn column, CsvRow row)
        {
            for (var nested = column.Nested; nested != null; nested = nested.Parent)
            {
                if (!row.TryRequired(nested.RequiredColumn, out _))
                {
                    return false;
                }
            }

            return true;
        }

        // Null means "the CSV said nothing about this", empty means "the author cleared it" — the
        // convention StoreIdOverrides and the webshop fields follow. Product details and pricing keep
        // their empty lists, so the inspector has something to add a first row to.
        static void NullOutEmptyCollections(CatalogItem item)
        {
            foreach (var group in s_Schema.RowGroups)
            {
                if (!group.NullWhenEmpty)
                {
                    continue;
                }

                var list = group.ReadList(item);
                if (list != null && list.Count == 0)
                {
                    group.Collection.SetValue(item, null);
                }
            }
        }

        static void AddAggregate(List<AssetState> issues, List<string> messages,
            string description, SeverityLevel level)
        {
            if (messages.Count == 0)
            {
                return;
            }

            var detail = new StringBuilder();
            foreach (var message in messages)
            {
                detail.Append("- ").AppendLine(message);
            }

            issues.Add(new AssetState(description, detail.ToString(), level, ParseStateType));
        }

        static void CheckDuplicate(bool isIdentical, string fieldName, string context,
            int rowNumber, int originalRow,
            List<string> duplicateMessages, List<string> conflictMessages)
        {
            if (isIdentical)
            {
                AddMessage(duplicateMessages, rowNumber, context, fieldName, "duplicate of", originalRow);
            }
            else
            {
                AddMessage(conflictMessages, rowNumber, context, fieldName, "conflicts with", originalRow);
            }
        }

        static void AddMessage(List<string> messages, int rowNumber, string context, string fieldName,
            string verb, int originalRow)
        {
            messages.Add($"Row {rowNumber}: ({context}) {fieldName} — {verb} row {originalRow}");
        }

        static Dictionary<string, int> BuildColumnMap(string[] headerFields)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headerFields.Length; i++)
            {
                var name = headerFields[i]?.Trim();
                if (!string.IsNullOrEmpty(name) && !map.ContainsKey(name))
                {
                    map[name] = i;
                }
            }

            return map;
        }

        static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.IndexOfAny(k_NeedsQuoting) >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        static List<string[]> ParseLines(string csvContent)
        {
            var results = new List<string[]>();
            var fields = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < csvContent.Length; i++)
            {
                var c = csvContent[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csvContent.Length && csvContent[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && i + 1 < csvContent.Length && csvContent[i + 1] == '\n')
                    {
                        i++;
                    }

                    fields.Add(current.ToString());
                    current.Clear();
                    results.Add(fields.ToArray());
                    fields.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            if (fields.Count > 0 || current.Length > 0)
            {
                fields.Add(current.ToString());
                results.Add(fields.ToArray());
            }

            return results;
        }

        /// <summary>
        /// One row, plus the header map that says which cell each column lives in. Conversion happens here
        /// so every column reports an unusable cell the same way.
        /// </summary>
        class CsvRow
        {
            readonly Dictionary<string, int> m_Columns;
            readonly List<AssetState> m_Issues;
            readonly HashSet<string> m_Reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] m_Fields;

            public int Number { get; private set; }

            public CsvRow(Dictionary<string, int> columns, List<AssetState> issues)
            {
                m_Columns = columns;
                m_Issues = issues;
            }

            public void Begin(string[] fields, int number)
            {
                m_Fields = fields;
                Number = number;
                m_Reported.Clear();
            }

            /// <summary>The cell as written, or an empty string when the file omits the column entirely.</summary>
            public string Raw(CsvColumn column)
            {
                if (!m_Columns.TryGetValue(column.Name, out var index) || index >= m_Fields.Length)
                {
                    return string.Empty;
                }

                return m_Fields[index]?.Trim() ?? string.Empty;
            }

            /// <summary>The converted cell, or the column's fallback when it is blank or unusable.</summary>
            public object Value(CsvColumn column)
            {
                var raw = Raw(column);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return column.Fallback;
                }

                if (column.Converter.TryFromCsv(raw, out var value))
                {
                    return value;
                }

                ReportInvalid(column, raw);
                return column.Fallback;
            }

            /// <summary>A cell that must be usable for its entry to exist; false leaves the entry unbuilt.</summary>
            public bool TryRequired(CsvColumn column, out object value)
            {
                var raw = Raw(column);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    value = null;
                    return false;
                }

                if (column.Converter.TryFromCsv(raw, out value) && value != null)
                {
                    // A cell spelling out the column's "not set" value leaves the entry unbuilt, the
                    // same as a blank one: a promotion of type None is no promotion.
                    if (column.IsAbsent(value))
                    {
                        value = null;
                        return false;
                    }

                    return true;
                }

                ReportInvalid(column, raw);
                value = null;
                return false;
            }

            void ReportInvalid(CsvColumn column, string raw)
            {
                var detail = column.Converter.InvalidValueDetail;
                if (detail == null)
                {
                    // Numbers and dates have always failed quietly: the entry drops and the row moves on.
                    return;
                }

                if (!m_Reported.Add(column.Name))
                {
                    // One complaint per cell, however many times the row reads it — a column that is
                    // both the key of its collection and part of the entry gets read twice.
                    return;
                }

                m_Issues.Add(new AssetState(
                    $"Row {Number}: unknown {column.Name} '{raw}'", detail,
                    SeverityLevel.Warning, ParseStateType));
            }
        }
    }
}
