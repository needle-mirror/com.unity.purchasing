using System;

namespace UnityEngine.Purchasing.PaymentProviderService
{
    [Serializable]
    internal struct ProductData
    {
        public string catalogListingId;
        public string unitySku;
        public string title;
        public string description;
        public string currency;
        public long priceInMicros;
        public string priceString;
        public string language;
    }
}
