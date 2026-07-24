#nullable enable

namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// Result of a native store-specific id → Unity uSku lookup via the backend store-overrides
    /// endpoint. Carries both the resolved uSku and the <see cref="ProductType"/> the backend
    /// reported for it, so callers can synthesize a Product with the correct type when the local
    /// cache doesn't have that uSku.
    /// </summary>
    internal record ResolvedUSku
    {
        internal string USku { get; }
        internal ProductType Type { get; }

        internal ResolvedUSku(string uSku, ProductType type)
        {
            USku = uSku;
            Type = type;
        }
    }
}
