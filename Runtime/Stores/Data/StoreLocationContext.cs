#nullable enable

namespace UnityEngine.Purchasing.Stores
{
    internal class StoreLocationContext : IStoreLocationContext
    {
        // Store callbacks write from their own threads (the App Store storefront callback runs
        // off the main thread) while checkouts read on others. One lock keeps every read of the
        // pair consistent with every write, including the two-field storefront update.
        readonly object m_Lock = new object();
        string? m_CountryCode;
        string? m_CurrencyCode;

        // Values are validated and uppercased here, for every writer (Google stores Play's
        // billing-config country and product currency as reported), so PlayerData's country
        // comparisons never depend on a store's casing. Malformed values are dropped.
        public string? CountryCode
        {
            get { lock (m_Lock) { return m_CountryCode; } }
            set { var country = NativeDeviceLocale.ValidAlpha2(value); lock (m_Lock) { m_CountryCode = country; } }
        }

        public string? CurrencyCode
        {
            get { lock (m_Lock) { return m_CurrencyCode; } }
            set { var currency = NativeDeviceLocale.ValidCurrency(value); lock (m_Lock) { m_CurrencyCode = currency; } }
        }

        public (string? CountryCode, string? CurrencyCode) Snapshot()
        {
            lock (m_Lock)
            {
                return (m_CountryCode, m_CurrencyCode);
            }
        }

        public void SetFromStorefront(string? alpha3CountryCode, string? currencyCode)
        {
            var alpha2 = NativeDeviceLocale.ValidAlpha2(IsoCountryCodeConverter.ToAlpha2(alpha3CountryCode));
            if (alpha2 == null)
            {
                return;
            }

            var currency = NativeDeviceLocale.ValidCurrency(currencyCode);

            lock (m_Lock)
            {
                var storefrontChanged = m_CountryCode != null && m_CountryCode != alpha2;
                m_CountryCode = alpha2;
                if (currency != null)
                {
                    m_CurrencyCode = currency;
                }
                else if (storefrontChanged)
                {
                    // No currency before iOS 17 / macOS 14. One taken from products belongs to the
                    // previous storefront, so drop it; PlayerData derives one from the new country.
                    m_CurrencyCode = null;
                }
            }
        }
    }
}
