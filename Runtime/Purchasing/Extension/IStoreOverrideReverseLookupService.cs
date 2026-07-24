#nullable enable
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.Extension
{
    internal interface IStoreOverrideReverseLookupService
    {
        /// <summary>
        /// Resolves a native store-specific id to its Unity uSku + ProductType via the backend
        /// store-overrides endpoint. Returns <c>null</c> when the backend has no mapping,
        /// when this project has no remote catalog, or when the id is null/empty.
        /// Callers are expected to look the returned uSku up in the local
        /// <see cref="IProductCache"/> — this service intentionally does not build or return
        /// a <see cref="Product"/>. The type is surfaced so callers can synthesize an accurate
        /// Product on cache miss.
        /// </summary>
        Task<ResolvedUSku?> ResolveAsync(string? nativeId);
    }
}
