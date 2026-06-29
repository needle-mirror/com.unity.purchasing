#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// One purchase choice for the cross-product picker. Carries the store to route
    /// through, the catalog listing id to buy, and an optional badge string to highlight
    /// a benefit ("20% more gems", "Best value", etc.).
    /// </summary>
    public readonly struct PurchaseOption
    {
        /// <summary>
        /// Store identifier this option routes through, or
        /// <see cref="IPaymentOptionProvider.NativeAlias"/> for the platform default.
        /// </summary>
        public string StoreName { get; }

        /// <summary>
        /// Catalog listing id to purchase via the chosen store. The SDK resolves this
        /// to the per-store override (when one exists) or the listing's uSku at order time.
        /// </summary>
        public string CatalogListingId { get; }

        /// <summary>
        /// Optional short label rendered as a callout on the option's button
        /// (e.g. "20% off", "Best value", "Includes bonus"). Null hides the badge.
        /// </summary>
        public string? Badge { get; }

        /// <summary>
        /// Constructs a purchase option.
        /// </summary>
        /// <param name="storeName">Store identifier this option routes through.</param>
        /// <param name="catalogListingId">Catalog listing id to purchase via the chosen store.</param>
        /// <param name="badge">Optional short label rendered as a callout on the button.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="storeName"/> or <paramref name="catalogListingId"/> is null.</exception>
        public PurchaseOption(string storeName, string catalogListingId, string? badge = null)
        {
            StoreName = storeName ?? throw new ArgumentNullException(nameof(storeName));
            CatalogListingId = catalogListingId ?? throw new ArgumentNullException(nameof(catalogListingId));
            Badge = badge;
        }
    }

    /// <summary>
    /// Contract for a UI flow that asks the user to pick a payment route, then resolves
    /// the chosen store name to a StoreController and invokes PurchaseProduct on it.
    /// Identification is by store-name string; <see cref="NativeAlias"/> is a sentinel that
    /// resolves to the platform's default store at click time.
    /// </summary>
    /// <remarks>
    /// Implementations live alongside the UI stack they render with:
    /// <c>PaymentOptionProvider</c> for UGUI, <c>PaymentOptionProviderUITK</c> for UI Toolkit.
    /// The UITK variant compiles only when the <c>com.unity.modules.uielements</c> module is
    /// installed in the project; projects without that module still get this interface and
    /// the UGUI implementation.
    /// </remarks>
    public interface IPaymentOptionProvider : IDisposable
    {
        /// <summary>
        /// Magic store-name value that resolves to the platform default
        /// (AppleAppStore / GooglePlay / fake in editor) at click time.
        /// Exists so callers can target the platform default without per-platform <c>#if</c> blocks.
        /// </summary>
        public const string NativeAlias = "native";

        /// <summary>
        /// Store-name value on the <see cref="PurchaseOption"/> returned when the user picked the webshop.
        /// </summary>
        public const string WebshopAlias = "webshop";

        /// <summary>
        /// Applies the picker's bundled dark theme. Rebuilds dynamic buttons so brand sprites
        /// swap to their light-on-dark tone variants. Triggers lazy init if the picker hasn't
        /// been initialized yet.
        /// </summary>
        void ApplyDarkTheme();

        /// <summary>
        /// Applies the picker's bundled light theme. Rebuilds dynamic buttons so brand sprites
        /// swap to their dark-on-light tone variants. Triggers lazy init if the picker hasn't
        /// been initialized yet.
        /// </summary>
        void ApplyLightTheme();

        /// <summary>
        /// Eligibility pre-check that mirrors what <see cref="ShowPurchaseOption(string)"/>
        /// would do: resolves the default purchase order and verifies the listing is priced
        /// in a single currency across all of those stores.
        /// </summary>
        /// <param name="catalogListingId">Catalog listing id to check.</param>
        /// <returns>True when every store in the resolved order carries the listing with a consistent currency.</returns>
        Task<bool> ValidatePurchaseOption(string catalogListingId);

        /// <summary>
        /// Eligibility pre-check for an explicit set of options: verifies every option's
        /// listing has a price and that all options share a currency.
        /// </summary>
        /// <param name="options">Options to validate.</param>
        /// <returns>True when every option resolves to a priced listing in a single shared currency.</returns>
        bool ValidatePurchaseOption(IReadOnlyList<PurchaseOption> options);

        /// <summary>
        /// Shows native + the first eligible payment provider (from the SDK's
        /// <c>GetEligiblePaymentProviders</c>). When only one store is eligible, routes
        /// directly without showing the modal.
        /// </summary>
        /// <param name="catalogListingId">Catalog listing id to purchase via the chosen store.</param>
        /// <returns>The chosen store name, or null if the user cancelled.</returns>
        Task<string?> ShowPurchaseOption(string catalogListingId);

        /// <summary>
        /// Cross-product picker. Each option specifies its own store + catalog listing id +
        /// optional badge text. On pick, resolves the chosen option's store and calls
        /// <c>PurchaseProduct</c> with its listing id (which the SDK resolves to the per-store
        /// override, or the uSku when no override exists). A single-option list bypasses the modal.
        /// </summary>
        /// <param name="options">Options to present to the user.</param>
        /// <returns>The chosen option, or null if the user cancelled.</returns>
        Task<PurchaseOption?> ShowPurchaseOption(IReadOnlyList<PurchaseOption> options);
    }
}
