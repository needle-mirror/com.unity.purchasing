#nullable enable
using System.Threading.Tasks;
using UnityEngine.Purchasing.PaymentProviderService.Models;

namespace UnityEngine.Purchasing.Stores
{
    internal interface IPlayerData
    {
        string DisplayName { get; set; }
        bool FirebaseDefaultAppConfigured { get; }

        /// <summary>
        /// Locale, country and currency for one checkout or catalog fetch, resolved from a
        /// single read of every source so the values can never mix sources.
        /// </summary>
        CheckoutLocation GetCheckoutLocation();

        Task<PlayerIdentity> CreatePlayerIdentityAsync(string? impressionId = null);
    }

    internal readonly struct CheckoutLocation
    {
        /// <summary>BCP-47 tag (e.g. "en-NG"), or null.</summary>
        public readonly string? Locale;

        /// <summary>ISO 3166-1 alpha-2 (e.g. "NG"), or null.</summary>
        public readonly string? CountryCode;

        /// <summary>ISO 4217 (e.g. "NGN"), or null — the backend then uses the first catalog currency.</summary>
        public readonly string? CurrencyCode;

        public CheckoutLocation(string? locale, string? countryCode, string? currencyCode)
        {
            Locale = locale;
            CountryCode = countryCode;
            CurrencyCode = currencyCode;
        }
    }
}
