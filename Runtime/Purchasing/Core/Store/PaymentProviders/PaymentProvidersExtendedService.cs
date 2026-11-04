#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.PaymentProviders
{
    class PaymentProvidersExtendedService : StoreService, IPaymentProvidersExtendedService
    {
        readonly IPaymentProvidersExtendedService m_PaymentProvidersExtendedService;

        [Preserve]
        internal PaymentProvidersExtendedService(IStoreConnectUseCase connectUseCase, IPaymentProvidersExtendedService paymentProvidersExtendedService, IStoreWrapper storeWrapper)
            : base(connectUseCase, storeWrapper)
        {
            m_PaymentProvidersExtendedService = paymentProvidersExtendedService;
        }

        public void SetCheckoutPresentationMode(CheckoutPresentationMode mode)
        {
            m_PaymentProvidersExtendedService.SetCheckoutPresentationMode(mode);
        }

        public void SetWebshopPresentationMode(CheckoutPresentationMode mode)
        {
            m_PaymentProvidersExtendedService.SetWebshopPresentationMode(mode);
        }

        public void SetWebViewLauncher(IWebViewLauncher? launcher)
        {
            m_PaymentProvidersExtendedService.SetWebViewLauncher(launcher);
        }

        public void SetDeepLinkScheme(string? scheme)
        {
            m_PaymentProvidersExtendedService.SetDeepLinkScheme(scheme);
        }

        public Task<EligiblePaymentProviders> GetEligiblePaymentProviders()
        {
            using (ImpressionScope.Shared.EnterNone())
            {
                return m_PaymentProvidersExtendedService.GetEligiblePaymentProviders();
            }
        }

        public Task<IReadOnlyDictionary<string, PurchaseEligibility>> GetPurchaseEligibility(IReadOnlyList<string>? catalogListingIds)
        {
            using (ImpressionScope.Shared.EnterNone())
            {
                return m_PaymentProvidersExtendedService.GetPurchaseEligibility(catalogListingIds);
            }
        }

        // Override the IPaymentProvidersStoreExtendedService default factories so the
        // modal receives the PaymentProvider purchase service's IPurchaseEventEmitter
        // and can fire PaymentOptionsShownEvent. Falls back to the parameterless
        // factory (no telemetry) if the purchase service hasn't been constructed
        // yet — modal still works, just no event.
        public IPaymentOptionProvider GetPaymentOptionProviderUGUI()
        {
            return new PaymentOptionProvider(ResolveEmitter());
        }

#if IAP_UIELEMENTS
        public IPaymentOptionProvider GetPaymentOptionProviderUITK(UnityEngine.UIElements.UIDocument? host = null)
        {
            return new PaymentOptionProviderUITK(host, ResolveEmitter());
        }
#endif

        static IPurchaseEventEmitter? ResolveEmitter()
        {
            var purchase = PurchaseServiceProvider.GetPurchaseService(PaymentProvider.Name);
            return (purchase as PaymentProvidersExtendedPurchaseService)?.PurchaseEventEmitter;
        }
    }
}
