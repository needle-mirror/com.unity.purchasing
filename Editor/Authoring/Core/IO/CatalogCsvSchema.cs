using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.IO
{
    /// <summary>
    /// A field or property reached by reflection. The catalog model uses both — CatalogItem exposes
    /// properties, its nested types plain fields — so the schema treats them alike.
    /// </summary>
    class CsvMember
    {
        readonly FieldInfo m_Field;
        readonly PropertyInfo m_Property;

        public CsvMember(MemberInfo member)
        {
            m_Field = member as FieldInfo;
            m_Property = member as PropertyInfo;
        }

        public string Name => m_Field != null ? m_Field.Name : m_Property.Name;

        public Type Type => m_Field != null ? m_Field.FieldType : m_Property.PropertyType;

        public object GetValue(object target) =>
            m_Field != null ? m_Field.GetValue(target) : m_Property.GetValue(target);

        public void SetValue(object target, object value)
        {
            if (m_Field != null)
            {
                m_Field.SetValue(target, value);
            }
            else
            {
                m_Property.SetValue(target, value);
            }
        }
    }

    /// <summary>
    /// One CSV column, bound to the model member it reads and writes.
    /// </summary>
    class CsvColumn
    {
        public string Name;
        public int Order;
        public bool Identity;
        public bool EmitWhenDefault;
        public object Fallback;
        public object TypeDefault;

        /// <summary>The value meaning the member isn't set, or null when every value of the type counts.</summary>
        public object Absent;

        /// <summary>The fallback the column declared, not the member's own default standing in for it.</summary>
        public object DeclaredFallback;
        public CsvMember Member;
        public ICsvValueConverter Converter;
        public CsvGroup Group;

        /// <summary>Nested object this column's member belongs to, or null when it sits on the row's own object.</summary>
        public CsvNested Nested;

        /// <summary>Reads through the nested object when there is one, yielding null if it doesn't exist.</summary>
        public object Read(object owner)
        {
            var target = Nested == null ? owner : Nested.Read(owner);
            return target == null ? null : Member.GetValue(target);
        }

        public bool IsAbsent(object value) => Absent != null && Absent.Equals(value);
    }

    /// <summary>
    /// A single nested object whose columns flatten into the owning row.
    /// </summary>
    class CsvNested
    {
        public CsvMember Member;
        public CsvColumn RequiredColumn;
        public CsvTarget Target;
        public CsvNested Parent;

        public object Read(object owner)
        {
            var target = Parent == null ? owner : Parent.Read(owner);
            return target == null ? null : Member.GetValue(target);
        }

        public object Create() => Activator.CreateInstance(Member.Type);

        public void Write(object owner, object value) => Member.SetValue(owner, value);
    }

    /// <summary>
    /// An object the parser fills from a row: its own columns, plus any nested objects hanging off it.
    /// </summary>
    class CsvTarget
    {
        public readonly List<CsvColumn> Columns = new List<CsvColumn>();
        public readonly List<CsvNested> Nested = new List<CsvNested>();

        public IEnumerable<CsvColumn> Flatten() =>
            Columns.Concat(Nested.SelectMany(n => n.Target.Flatten()));
    }

    /// <summary>
    /// Either the item itself or one of the collections the CSV pivots into rows.
    /// </summary>
    class CsvGroup
    {
        public string Name;
        public int Index;
        public CsvTarget Target;
        public CsvMember Collection;
        public Type ElementType;
        public CsvColumn KeyColumn;
        public CsvColumn[] RequiredColumns;
        public CsvColumn[] ComparedColumns;
        public bool ReportDuplicates;
        public bool NullWhenEmpty;
        public bool IgnoreKeyCase;

        /// <summary>A <c>List&lt;string&gt;</c> has no members to annotate, so the cell is the whole entry.</summary>
        public bool ScalarElement;

        public bool IsItem => Collection == null;

        public IList ReadList(object item) => (IList)Collection.GetValue(item);

        public IList CreateList(object item)
        {
            var list = (IList)Activator.CreateInstance(Collection.Type);
            Collection.SetValue(item, list);
            return list;
        }

        public object CreateElement() => Activator.CreateInstance(ElementType);
    }

    /// <summary>
    /// The CSV shape of a catalog model, discovered from its <see cref="CsvColumnAttribute"/> annotations.
    /// Annotating a property adds a column here, and <c>CatalogCsvParser</c> reads and writes it without
    /// naming it. <see cref="Catalog"/> is the shape of the real model; the constructor is open so the
    /// discovery rules can be exercised against a model of their own.
    /// </summary>
    class CatalogCsvSchema
    {
        const BindingFlags k_Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>The shape of <see cref="CatalogItem"/>, built once.</summary>
        public static CatalogCsvSchema Catalog { get; } = new CatalogCsvSchema(typeof(CatalogItem));

        /// <summary>The item itself: columns repeated on every row of the item.</summary>
        public CsvGroup Item { get; }

        /// <summary>Collections that pivot into rows, in column order.</summary>
        public CsvGroup[] RowGroups { get; }

        /// <summary>Every column, in the order <c>Serialize</c> writes them.</summary>
        public CsvColumn[] Columns { get; }

        /// <summary>Item columns compared across the rows of one item, to report contradictions.</summary>
        public CsvColumn[] ItemConflictColumns { get; }

        public string Header { get; }

        public CatalogCsvSchema(Type itemType)
        {
            var all = new List<CsvColumn>();
            var rowGroups = new List<CsvGroup>();

            Item = new CsvGroup { Target = new CsvTarget(), ReportDuplicates = false };
            Walk(itemType, Item.Target, null, Item, all, rowGroups);
            foreach (var column in Item.Target.Flatten())
            {
                column.Group = Item;
            }

            RowGroups = rowGroups
                .OrderBy(g => g.Target.Flatten().Min(c => c.Order))
                .ToArray();
            for (var i = 0; i < RowGroups.Length; i++)
            {
                RowGroups[i].Index = i;
            }

            Columns = all.OrderBy(c => c.Order).ToArray();
            ItemConflictColumns = Item.Target.Flatten()
                .Where(c => !c.Identity)
                .OrderBy(c => c.Order)
                .ToArray();
            Header = string.Join(",", Columns.Select(c => c.Name));

            Validate();
        }

        /// <summary>A column the parser reads and writes itself, looked up by the member it belongs to.</summary>
        public CsvColumn Identity(string memberName)
        {
            var column = Columns.FirstOrDefault(c => c.Identity && c.Member.Name == memberName);
            if (column == null)
            {
                throw new InvalidOperationException(
                    $"{memberName} must carry a [CsvColumn] with Identity = true.");
            }

            return column;
        }

        // Duplicate names would make a column unreadable (the header map keeps the first); duplicate
        // orders would make the header order depend on reflection order, and the header is a
        // compatibility surface — existing files, the documented samples and the CLI goldens all
        // depend on it. Fail loudly at load rather than silently shuffling columns.
        void Validate()
        {
            var duplicateName = Columns
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateName != null)
            {
                throw new InvalidOperationException(
                    $"Duplicate CSV column name '{duplicateName.Key}' on the catalog model.");
            }

            var duplicateOrder = Columns.GroupBy(c => c.Order).FirstOrDefault(g => g.Count() > 1);
            if (duplicateOrder != null)
            {
                throw new InvalidOperationException(
                    $"Duplicate CSV column order {duplicateOrder.Key} on the catalog model, shared by " +
                    $"{string.Join(" and ", duplicateOrder.Select(c => c.Name))}.");
            }
        }

        static void Walk(Type type, CsvTarget target, CsvNested parent, CsvGroup owner,
            List<CsvColumn> all, List<CsvGroup> rowGroups)
        {
            foreach (var member in type.GetFields(k_Members).Cast<MemberInfo>().Concat(type.GetProperties(k_Members)))
            {
                var rowCollection = member.GetCustomAttribute<CsvRowCollectionAttribute>();
                var nested = member.GetCustomAttribute<CsvNestedAttribute>();
                var column = member.GetCustomAttribute<CsvColumnAttribute>();

                if (rowCollection != null)
                {
                    if (owner == null || !owner.IsItem || parent != null)
                    {
                        throw new InvalidOperationException(
                            $"[CsvRowCollection] on {type.Name}.{member.Name} is only supported on CatalogItem — " +
                            "a row can carry one entry per collection, not a grid.");
                    }

                    rowGroups.Add(BuildRowGroup(new CsvMember(member), rowCollection, column, all));
                    continue;
                }

                if (nested != null)
                {
                    var child = new CsvNested
                    {
                        Member = new CsvMember(member),
                        Target = new CsvTarget(),
                        Parent = parent,
                    };
                    Walk(child.Member.Type, child.Target, child, owner, all, rowGroups);
                    child.RequiredColumn = Require(child.Target, nested.RequiredMember, member);
                    target.Nested.Add(child);
                    continue;
                }

                if (column != null)
                {
                    var built = BuildColumn(new CsvMember(member), column, parent);
                    target.Columns.Add(built);
                    all.Add(built);
                }
            }
        }

        static CsvGroup BuildRowGroup(CsvMember member, CsvRowCollectionAttribute attribute,
            CsvColumnAttribute column, List<CsvColumn> all)
        {
            var group = new CsvGroup
            {
                Name = member.Name,
                Collection = member,
                ElementType = member.Type.GetGenericArguments().Single(),
                Target = new CsvTarget(),
                ReportDuplicates = attribute.ReportDuplicates,
                NullWhenEmpty = attribute.NullWhenEmpty,
                IgnoreKeyCase = attribute.IgnoreKeyCase,
            };

            if (column != null)
            {
                // The collection member carries the column itself, so the entry is the converted cell.
                // Nothing but the key exists to compare, which is why these lists dedupe in silence.
                // Typed by the element, not the list: the cell holds one category, not all of them.
                var cell = BuildColumn(member, column, null, group.ElementType);
                cell.Group = group;
                group.ScalarElement = true;
                group.KeyColumn = cell;
                group.RequiredColumns = new[] { cell };
                group.ComparedColumns = Array.Empty<CsvColumn>();
                group.Target.Columns.Add(cell);
                all.Add(cell);
                return group;
            }

            Walk(group.ElementType, group.Target, null, group, all, new List<CsvGroup>());
            foreach (var owned in group.Target.Flatten())
            {
                owned.Group = group;
            }

            group.KeyColumn = Require(group.Target, attribute.KeyMember, null);
            group.RequiredColumns = attribute.RequiredMembers == null
                ? new[] { group.KeyColumn }
                : attribute.RequiredMembers.Select(m => Require(group.Target, m, null)).ToArray();
            group.ComparedColumns = group.Target.Flatten()
                .Where(c => c != group.KeyColumn)
                .OrderBy(c => c.Order)
                .ToArray();
            return group;
        }

        static CsvColumn Require(CsvTarget target, string memberName, MemberInfo context)
        {
            var column = target.Flatten().FirstOrDefault(c => c.Member.Name == memberName);
            if (column == null)
            {
                var where = context == null ? "the collection's element type" : $"{context.Name}'s type";
                throw new InvalidOperationException(
                    $"'{memberName}' names no [CsvColumn] member on {where}.");
            }

            return column;
        }

        static CsvColumn BuildColumn(CsvMember member, CsvColumnAttribute attribute, CsvNested nested,
            Type valueType = null)
        {
            // valueType differs from the member's own type only for a scalar row collection, where the
            // member is the List and the cell is one of its elements.
            var type = valueType ?? member.Type;
            var declared = Coerce(type, attribute.Fallback);
            return new CsvColumn
            {
                Name = attribute.Name,
                Order = attribute.Order,
                Identity = attribute.Identity,
                EmitWhenDefault = attribute.EmitWhenDefault,
                // A blank cell falls back to what the column declared; failing that, to the member's own
                // default, so a non-nullable member is never handed a null.
                Fallback = declared ?? DefaultOf(type),
                DeclaredFallback = declared,
                TypeDefault = DefaultOf(type),
                Absent = Coerce(type, attribute.Absent),
                Member = member,
                Nested = nested,
                // The converter sees only the declared fallback: a column with nowhere to fall back to
                // says "Expected one of: ..." where one with a default says "Defaulting to ...".
                Converter = CreateConverter(attribute.Converter, type, declared),
            };
        }

        static ICsvValueConverter CreateConverter(Type declared, Type memberType, object fallback)
        {
            if (declared != null)
            {
                var contextual = declared.GetConstructor(new[] { typeof(Type), typeof(object) });
                return (ICsvValueConverter)(contextual != null
                    ? contextual.Invoke(new[] { memberType, fallback })
                    : Activator.CreateInstance(declared));
            }

            var type = Nullable.GetUnderlyingType(memberType) ?? memberType;
            if (type == typeof(string))
            {
                return new CsvStringConverter();
            }

            if (type == typeof(double))
            {
                return new CsvDoubleConverter();
            }

            if (type == typeof(bool))
            {
                return new CsvBoolConverter();
            }

            if (type == typeof(DateTimeOffset))
            {
                return new CsvDateTimeOffsetConverter();
            }

            if (type.IsEnum)
            {
                return new CsvEnumConverter(memberType, fallback);
            }

            throw new InvalidOperationException(
                $"No built-in CSV conversion for {memberType.Name}. Give the column a Converter.");
        }

        // Attribute arguments are constants, so a fallback can arrive as the wrong numeric type
        // (Fallback = 0 for a double member). Coerce rather than throw at first use.
        static object Coerce(Type memberType, object value)
        {
            if (value == null)
            {
                return null;
            }

            var target = Nullable.GetUnderlyingType(memberType) ?? memberType;
            if (value.GetType() == target)
            {
                return value;
            }

            return target.IsEnum
                ? Enum.ToObject(target, value)
                : Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        }

        static object DefaultOf(Type type) =>
            type.IsValueType && Nullable.GetUnderlyingType(type) == null
                ? Activator.CreateInstance(type)
                : null;
    }
}
