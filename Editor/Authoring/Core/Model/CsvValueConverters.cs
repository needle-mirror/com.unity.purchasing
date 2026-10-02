using System;
using System.Globalization;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    /// <summary>
    /// Plain text. The reader has already trimmed the cell and never passes a blank one on, so this hands
    /// back exactly what the file said.
    /// </summary>
    class CsvStringConverter : ICsvValueConverter
    {
        public string InvalidValueDetail => null;

        public string ToCsv(object value) => (string)value ?? string.Empty;

        public bool TryFromCsv(string text, out object value)
        {
            value = text;
            return true;
        }
    }

    /// <summary>
    /// Invariant-culture number, so a catalog authored in one locale reads the same in another. Rejection
    /// is silent: a price cell that isn't a number has always dropped its entry without comment.
    /// </summary>
    class CsvDoubleConverter : ICsvValueConverter
    {
        public string InvalidValueDetail => null;

        public virtual string ToCsv(object value) =>
            ((double)value).ToString("G", CultureInfo.InvariantCulture);

        public bool TryFromCsv(string text, out object value)
        {
            var parsed = double.TryParse(
                text, NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out var result);
            value = result;
            return parsed;
        }
    }

    /// <summary>
    /// A price that doubles as its own "is it set" flag: Unity's inspector can't render Nullable&lt;T&gt;,
    /// so anything below <see cref="PricingDetails.WebshopPriceUnsetThreshold"/> means unset and writes a
    /// blank cell. Sub-cent values aren't a real commercial price.
    /// </summary>
    class CsvWebshopPriceConverter : CsvDoubleConverter
    {
        public override string ToCsv(object value) =>
            (double)value >= PricingDetails.WebshopPriceUnsetThreshold
                ? base.ToCsv(value)
                : string.Empty;
    }

    /// <summary>
    /// "true" or a blank cell. Anything else reads as false, quietly, the way the webshop toggle always has.
    /// The blank comes from <see cref="CsvColumnAttribute.EmitWhenDefault"/>, not from here.
    /// </summary>
    class CsvBoolConverter : ICsvValueConverter
    {
        public string InvalidValueDetail => null;

        public string ToCsv(object value) => (bool)value ? "true" : "false";

        public bool TryFromCsv(string text, out object value)
        {
            var parsed = bool.TryParse(text, out var result);
            value = parsed && result;
            return true;
        }
    }

    /// <summary>
    /// Round-trip ("o") timestamps on write, anything <c>DateTimeOffset.TryParse</c> accepts on read —
    /// hand-authored cells rarely carry the seven fractional digits the round-trip format emits.
    /// </summary>
    class CsvDateTimeOffsetConverter : ICsvValueConverter
    {
        public string InvalidValueDetail => null;

        public string ToCsv(object value) =>
            ((DateTimeOffset?)value)?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty;

        public bool TryFromCsv(string text, out object value)
        {
            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var result))
            {
                value = result;
                return true;
            }

            value = null;
            return false;
        }
    }

    /// <summary>
    /// Enum names, matched case-insensitively. Unlike the numeric and date converters this one explains a
    /// rejection, because an unrecognized name is a typo the author can fix — and the explanation depends on
    /// whether the column has somewhere to fall back to.
    /// </summary>
    class CsvEnumConverter : ICsvValueConverter
    {
        // Enum.GetNames and Enum.GetValues are both ordered by the constants' binary values, so an index
        // into one indexes the other. Cached because this runs per cell, per row, on every asset import.
        readonly string[] m_Names;
        readonly Array m_Values;
        readonly object m_Fallback;

        public CsvEnumConverter(Type memberType, object fallback)
        {
            var enumType = Nullable.GetUnderlyingType(memberType) ?? memberType;
            m_Names = Enum.GetNames(enumType);
            m_Values = Enum.GetValues(enumType);
            m_Fallback = fallback;
        }

        public string InvalidValueDetail =>
            m_Fallback != null
                ? $"Defaulting to {ToCsv(m_Fallback)}."
                : $"Expected one of: {string.Join(", ", m_Names)}.";

        public virtual string ToCsv(object value) => value?.ToString() ?? string.Empty;

        public virtual bool TryFromCsv(string text, out object value)
        {
            for (var i = 0; i < m_Names.Length; i++)
            {
                if (string.Equals(m_Names[i], text, StringComparison.OrdinalIgnoreCase))
                {
                    value = m_Values.GetValue(i);
                    return true;
                }
            }

            // A spreadsheet that coerced an enum column to numbers still reads, as it always has —
            // but only for a number the enum actually defines. An out-of-range one is a typo, not a
            // value, and saying so beats storing an enum with no name.
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ordinal))
            {
                for (var i = 0; i < m_Values.Length; i++)
                {
                    var candidate = m_Values.GetValue(i);
                    if (Convert.ToInt64(candidate, CultureInfo.InvariantCulture) == ordinal)
                    {
                        value = candidate;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }
    }

    /// <summary>
    /// Locales, spelled the way the schema spells them. The schema follows BCP-47 and hyphenates ("en-US");
    /// the C# enum name can't, so it uses an underscore. Written hyphenated, read either way.
    /// </summary>
    class CsvLocaleConverter : CsvEnumConverter
    {
        public CsvLocaleConverter(Type memberType, object fallback)
            : base(memberType, fallback)
        {
        }

        public override string ToCsv(object value) => base.ToCsv(value).Replace('_', '-');

        public override bool TryFromCsv(string text, out object value) =>
            base.TryFromCsv(text.Replace('-', '_'), out value);
    }
}
