namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    /// <summary>
    /// Converts one CSV cell to and from a model value, for members whose text form doesn't follow the
    /// built-in conversion for their CLR type. Point a <see cref="CsvColumnAttribute.Converter"/> at an
    /// implementation; it is instantiated once per column and must be stateless.
    /// </summary>
    /// <remarks>
    /// Values are boxed because columns are reached through reflection. Implementations never see a blank
    /// cell — the parser substitutes <see cref="CsvColumnAttribute.Fallback"/> for those without calling
    /// the converter.
    /// </remarks>
    interface ICsvValueConverter
    {
        /// <summary>Renders a value as an unescaped CSV field; an empty string leaves the cell blank.</summary>
        string ToCsv(object value);

        /// <summary>
        /// Parses a non-blank cell; false falls back to <see cref="CsvColumnAttribute.Fallback"/>.
        /// </summary>
        bool TryFromCsv(string text, out object value);

        /// <summary>
        /// Detail line for the warning raised when <see cref="TryFromCsv"/> rejects a cell; null reports nothing.
        /// </summary>
        string InvalidValueDetail { get; }
    }
}
