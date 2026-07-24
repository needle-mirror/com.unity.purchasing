#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Purchasing.PaymentProviderService.Models;

namespace UnityEngine.Purchasing.Stores
{
    /// <summary>
    /// Test seam over the backend store-overrides endpoint. The production impl
    /// (<see cref="StoreOverrideLookupClient"/>) self-constructs the IAP API client lazily
    /// from <c>CoreRegistry</c>; tests inject their own implementation.
    /// </summary>
    internal interface IStoreOverrideLookupClient
    {
        /// <summary>
        /// Fetches every store-specific id override for the given store, keyed by
        /// store-specific id.
        /// </summary>
        Task<Dictionary<string, StoreOverrideMatch>> Lookup(string store);
    }
}
