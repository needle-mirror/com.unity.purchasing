#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.WebshopService
{
    internal interface IWebshopService
    {
        bool RestrictedTokensAvailable();
        Task<WebshopLinkData> GetWebshopLink(
            string? catalogListingId,
            string? impressionId,
            string? locale,
            string? currencyCode,
            string? country,
            IReadOnlyList<WebshopExternalToken> externalTokens);
    }
}
