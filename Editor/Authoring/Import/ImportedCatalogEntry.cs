using System;

namespace UnityEditor.Purchasing.Editor.Authoring.Import
{
    /// <summary>
    /// Represents a single catalog entry imported from an external store.
    /// Shared across all auth/fetch providers.
    /// </summary>
    [Serializable]
    class ImportedCatalogEntry
    {
        public string Sku;
        public string Title;
        public string Description;
        public string Language;
        public string ProductType;
        public string CurrencyCode;
        public double Price;
        public string ImageUrl;
    }
}
