#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A public interface for the Payment Providers Store service extension.
    /// </summary>
    public interface IPaymentProvidersExtendedService : IStoreServiceExtension
    {
        /// <summary>
        /// Set how payment-provider checkout URLs are presented.
        /// Defaults to <see cref="CheckoutPresentationMode.ExternalBrowser"/>.
        /// </summary>
        void SetCheckoutPresentationMode(CheckoutPresentationMode mode);

        /// <summary>
        /// Set how webshop redirect URLs are presented (the
        /// <see cref="IPaymentProvidersExtendedPurchaseService.RedirectToWebshop"/> flow).
        /// Independent of <see cref="SetCheckoutPresentationMode"/>; defaults to
        /// <see cref="CheckoutPresentationMode.ExternalBrowser"/>.
        /// </summary>
        void SetWebshopPresentationMode(CheckoutPresentationMode mode);

        /// <summary>
        /// Register a custom <see cref="IWebViewLauncher"/> for use when
        /// <see cref="CheckoutPresentationMode.WebView"/> is selected. Pass
        /// <c>null</c> to re-enable the platform's built-in launcher
        /// (SFSafariViewController on iOS, Chrome Custom Tabs on Android).
        /// </summary>
        void SetWebViewLauncher(IWebViewLauncher? launcher);

        /// <summary>
        /// Configure a deep-link URL scheme (e.g. <c>"myapp"</c>) that the
        /// SDK will listen for while a webview checkout is open. When a deep
        /// link with this scheme arrives, the SDK dismisses the in-app
        /// browser and resolves the checkout immediately. Pass <c>null</c>
        /// to disable deep-link handling.
        /// </summary>
        void SetDeepLinkScheme(string? scheme);

        /// <summary>
        /// Returns the payment provider identifiers the calling player is eligible for, in priority order
        /// (highest first), along with a server-driven killswitch for the payment-options popup.
        /// Use <see cref="EligiblePaymentProviders.Providers"/>[0] as the default choice and pass it to
        /// <see cref="IPaymentProvidersExtendedPurchaseService.SetPaymentProviderOverride"/>.
        /// An empty providers list means no provider is eligible for this player — treat it as a normal
        /// "external payment unavailable" state, not an error.
        /// <see cref="EligiblePaymentProviders.PaymentOptionPopupEnabled"/> defaults to true when the
        /// backend omits the field; suppress the picker UI only when the server explicitly returns false.
        /// </summary>
        Task<EligiblePaymentProviders> GetEligiblePaymentProviders();

        /// <summary>
        /// Checks whether the calling player can buy each catalog listing now, given the listing's purchase limit.
        /// Entitlements are not considered. Accepts 1 to 50 catalog listing ids.
        /// Does not throw: if the request fails, every id maps to a result with <see cref="PurchaseEligibility.Eligible"/> false
        /// and <see cref="PurchaseEligibilityFailureReason.RequestFailed"/>.
        /// </summary>
        /// <param name="catalogListingIds">The catalog listing ids to check.</param>
        /// <returns>Results keyed by catalog listing id, with an entry for every id passed in. Empty when <paramref name="catalogListingIds"/> is null or empty.</returns>
        Task<IReadOnlyDictionary<string, PurchaseEligibility>> GetPurchaseEligibility(IReadOnlyList<string>? catalogListingIds);

        /// <summary>
        /// Returns a new UGUI payment-method picker. View hierarchy is built lazily on first use;
        /// caller owns the returned instance and must call <see cref="System.IDisposable.Dispose"/>
        /// when done. The picker creates a screen-space-overlay Canvas with sortingOrder 10000;
        /// projects that layer UI above that range can tweak it after first use by finding the
        /// "PaymentOptionProvider" GameObject and overriding the Canvas sortingOrder directly.
        /// </summary>
        /// <remarks>
        /// On Apple platforms with Unity 6, the "Continue with" text before the Apple In-App Purchase
        /// mark is drawn in the system font's medium weight, as Apple's guidelines for the mark require,
        /// through TextMesh Pro. That needs the project's TextMesh Pro resources: import them with
        /// <b>Window &gt; TextMeshPro &gt; Import TMP Essential Resources</b>. Without them, and with
        /// Unity 2022.3, the text is drawn in the regular weight. The UI Toolkit picker
        /// (<c>GetPaymentOptionProviderUITK()</c>) draws the medium weight without them.
        /// </remarks>
        IPaymentOptionProvider GetPaymentOptionProviderUGUI() => new PaymentOptionProvider();

#if IAP_UIELEMENTS
        /// <summary>
        /// Returns a new UI Toolkit payment-method picker. View hierarchy is built lazily on first
        /// use, on the picker's own panel: a "PaymentOptionProviderUITK" GameObject (kept across
        /// scene loads) with a <see cref="UnityEngine.UIElements.UIDocument"/> and its own
        /// <see cref="UnityEngine.UIElements.PanelSettings"/>. The panel is scaled so the picker keeps
        /// a consistent physical size across screen densities, follows screen resizes and
        /// orientation changes, and shrinks to fit screens too small for it. Caller owns the returned
        /// instance and must call <see cref="System.IDisposable.Dispose"/> when done, which also
        /// destroys that GameObject.
        /// The GameObject exists once <c>ApplyLightTheme</c>, <c>ApplyDarkTheme</c>,
        /// <c>ApplyBottomAnchoredLayout</c>, <c>ApplyCenteredLayout</c>, <c>ApplyAutomaticLayout</c> or
        /// <c>ValidatePurchaseOption</c> has been called, so to customize the picker call one of those
        /// first, then find the GameObject by name, then call <c>ShowPurchaseOption</c>.
        /// If several pickers are alive at once, <c>GameObject.Find</c> returns any one of them.
        /// The panel's sortingOrder is 10000, which places it above Screen Space - Overlay canvases
        /// and other panels with a lower sortingOrder; projects that layer UI above that range can
        /// raise it through the GameObject's <see cref="UnityEngine.UIElements.UIDocument.panelSettings"/>.
        /// Because the picker does not render inside the game's panels, the game's theme and style
        /// sheets do not apply to it. To override the picker's USS variables, add a style sheet to
        /// that UIDocument's <see cref="UnityEngine.UIElements.UIDocument.rootVisualElement"/>.
        /// Only available when the <c>com.unity.modules.uielements</c> module is referenced by the project.
        /// </summary>
        /// <returns>A new picker instance.</returns>
        IPaymentOptionProvider GetPaymentOptionProviderUITK() => new PaymentOptionProviderUITK();

        /// <summary>
        /// Obsolete: the UI Toolkit picker now renders on its own panel, so <paramref name="host"/>
        /// is ignored. Use <see cref="GetPaymentOptionProviderUITK()"/> instead.
        /// </summary>
        /// <param name="host">Ignored.</param>
        /// <returns>A new picker instance, the same as <see cref="GetPaymentOptionProviderUITK()"/>.</returns>
        [System.Obsolete("The UI Toolkit picker now renders on its own panel and ignores the host UIDocument. " +
            "Use GetPaymentOptionProviderUITK() instead.")]
        IPaymentOptionProvider GetPaymentOptionProviderUITK(UnityEngine.UIElements.UIDocument? host = null)
            => GetPaymentOptionProviderUITK();
#endif
    }
}
