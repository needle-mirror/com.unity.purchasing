#nullable enable

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
        /// Returns a new UGUI payment-method picker. View hierarchy is built lazily on first use;
        /// caller owns the returned instance and must call <see cref="System.IDisposable.Dispose"/>
        /// when done. The picker creates a screen-space-overlay Canvas with sortingOrder 10000;
        /// projects that layer UI above that range can tweak it after first use by finding the
        /// "PaymentOptionProvider" GameObject and overriding the Canvas sortingOrder directly.
        /// </summary>
        IPaymentOptionProvider GetPaymentOptionProviderUGUI() => new PaymentOptionProvider();

#if IAP_UIELEMENTS
        /// <summary>
        /// Returns a new UI Toolkit payment-method picker. View hierarchy is built lazily on first
        /// use and appended to a UIDocument's <c>rootVisualElement</c>; caller owns the returned
        /// instance and must call <see cref="System.IDisposable.Dispose"/> when done.
        /// Only available when the <c>com.unity.modules.uielements</c> module is referenced by the project.
        /// </summary>
        /// <param name="host">UIDocument the picker should attach to. Pass <c>null</c> (default) to
        /// auto-discover the first active UIDocument in the scene; in scenes with multiple active
        /// documents, pass one explicitly to guarantee the picker renders under the right one.</param>
        IPaymentOptionProvider GetPaymentOptionProviderUITK(UnityEngine.UIElements.UIDocument? host = null)
            => new PaymentOptionProviderUITK(host);
#endif
    }
}
