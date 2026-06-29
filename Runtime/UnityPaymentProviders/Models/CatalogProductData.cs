using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.PaymentProviderService
{
    [Serializable]
    internal struct CatalogProductData
    {
        public string catalogListingId;
        public string unitySku;
        public CatalogProductType productType;
        public List<CatalogStoreOverride> storeOverrides;
    }

    [Serializable]
    internal struct CatalogStoreOverride
    {
        public string storeName;
        public string skuOverride;
    }

    internal enum CatalogProductType
    {
        Consumable,
        NonConsumable,
        Subscription,
        Unknown
    }
}
