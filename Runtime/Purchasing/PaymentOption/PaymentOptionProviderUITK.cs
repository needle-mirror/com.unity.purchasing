#if IAP_UIELEMENTS
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace UnityEngine.Purchasing
{
    // UI Toolkit implementation of IPaymentOptionProvider. View surface only — all state,
    // validation, and routing live in PurchaseOptionViewModel. Appends its modal tree under
    // a UIDocument's rootVisualElement so theme and scaling propagate without configuration.
    // The host UIDocument is either passed at construction (multi-document scenes) or
    // auto-discovered on first use via FindAnyObjectByType<UIDocument>(). Constructed via
    // IPaymentProvidersStoreExtendedService.GetPaymentOptionProviderUITK() / (UIDocument).
    internal sealed partial class PaymentOptionProviderUITK : IPaymentOptionProvider
    {
        PurchaseOptionViewModel? m_VM;
        UIDocument? m_HostOverride;
        readonly IPurchaseEventEmitter? m_Emitter;

        VisualElement? m_Modal;
        VisualElement? m_Overlay;
        VisualElement? m_Sheet;
        Button? m_CloseX;

        bool m_Initialized;
        bool m_Disposed;
        bool m_DarkMode;

        readonly List<Button> m_DynamicButtons = new();

        internal PaymentOptionProviderUITK(UIDocument? host = null, IPurchaseEventEmitter? emitter = null)
        {
            m_HostOverride = host;
            m_VM = new PurchaseOptionViewModel();
            m_Emitter = emitter;
        }

        void IPaymentOptionProvider.ApplyDarkTheme()
        {
            EnsureInitialized();
            m_DarkMode = true;
            m_Modal!.AddToClassList(k_BlockDark);
            if (m_CurrentOpts != null && m_DynamicButtons.Count > 0)
                BuildButtons(m_CurrentOpts);
        }

        void IPaymentOptionProvider.ApplyLightTheme()
        {
            EnsureInitialized();
            m_DarkMode = false;
            m_Modal!.RemoveFromClassList(k_BlockDark);
            if (m_CurrentOpts != null && m_DynamicButtons.Count > 0)
                BuildButtons(m_CurrentOpts);
        }

        Task<bool> IPaymentOptionProvider.ValidatePurchaseOption(string catalogListingId)
        {
            EnsureInitialized();
            return m_VM!.ValidatePurchaseOption(catalogListingId);
        }

        bool IPaymentOptionProvider.ValidatePurchaseOption(IReadOnlyList<PurchaseOption> options)
        {
            EnsureInitialized();
            return m_VM!.ValidatePurchaseOption(options);
        }

        async Task<string?> IPaymentOptionProvider.ShowPurchaseOption(string catalogListingId)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(catalogListingId))
                throw new ArgumentException("catalogListingId required", nameof(catalogListingId));

            var storeOptions = await m_VM!.BuildDefaultOptions(catalogListingId);
            if (storeOptions.Count == 0 && !m_VM.HasWebshop(catalogListingId))
            {
                Debug.unityLogger.LogIAPWarning(
                    $"NoEligibleStore: no store has {catalogListingId} cached and no webshop available; returning null");
                return null;
            }
            var task = m_VM.BeginShowPurchaseOptionForListing(storeOptions, catalogListingId, out var opts);
            if (opts.Count > 0)
            {
                BuildButtons(opts);
                m_Overlay!.RemoveFromClassList(k_OverlayHidden);
                EmitOptionsShown(opts);
            }
            var chosen = await task;
            // Drop any staged impression_id if the modal closed without a pick;
            // no-op when the user picked (PurchaseIntentStart already consumed it).
            ImpressionIdContext.Clear();
            return chosen?.StoreName;
        }

        async Task<PurchaseOption?> IPaymentOptionProvider.ShowPurchaseOption(IReadOnlyList<PurchaseOption> options)
        {
            EnsureInitialized();
            var task = m_VM!.BeginShowPurchaseOption(options, out var opts);
            if (opts.Count > 0)
            {
                BuildButtons(opts);
                m_Overlay!.RemoveFromClassList(k_OverlayHidden);
                EmitOptionsShown(opts);
            }
            var chosen = await task;
            ImpressionIdContext.Clear();
            return chosen;
        }

        // Same mapping as the UGUI provider; see PaymentOptionProvider.EmitOptionsShown.
        void EmitOptionsShown(IReadOnlyList<PurchaseOptionViewModel.PurchaseOpt> opts)
        {
            if (m_Emitter == null) return;

            var mapped = new List<PaymentOption>(opts.Count);
            foreach (var opt in opts)
            {
                if (opt.IsWebshop) mapped.Add(PaymentOption.Webshop);
                else if (opt.IsNative) mapped.Add(PaymentOption.Native);
                else if (PaymentBrandRegistry.IsStripe(opt.StoreName)) mapped.Add(PaymentOption.Stripe);
                else if (PaymentBrandRegistry.IsCoda(opt.StoreName)) mapped.Add(PaymentOption.Codapay);
            }

            m_Emitter.SendPaymentOptionsShownEvent(mapped, defaultProvider: null);
        }

        void IDisposable.Dispose()
        {
            if (m_Disposed) return;
            m_VM?.Dispose();
            m_VM = null;
            ClearDynamicButtons();
            m_CurrentOpts = null;
            m_Modal?.RemoveFromHierarchy();
            m_Modal = null;
            m_Overlay = null;
            m_Sheet = null;
            m_CloseX = null;
            m_Initialized = false;
            m_Disposed = true;
        }

        void EnsureInitialized()
        {
            if (m_Disposed) throw new InvalidOperationException("PaymentOptionProviderUITK has been disposed");
            if (m_Initialized) return;

            // Prefer the caller-supplied host; fall back to first active UIDocument in the
            // scene. Auto-discovery is non-deterministic when multiple UIDocuments are
            // present — callers in those scenes should pass an explicit host via
            // IPaymentProvidersStoreExtendedService.GetPaymentOptionProviderUITK(UIDocument).
            var hostDocument = m_HostOverride != null
                ? m_HostOverride
                : Object.FindAnyObjectByType<UIDocument>();
            if (hostDocument == null)
            {
                throw new InvalidOperationException(
                    "PaymentOptionProviderUITK has no host UIDocument: " +
                    "pass one to GetPaymentOptionProviderUITK(UIDocument) or ensure " +
                    "an active UIDocument exists in the scene.");
            }
            var hostRoot = hostDocument.rootVisualElement;
            if (hostRoot == null)
            {
                throw new InvalidOperationException(
                    "Host UIDocument has no rootVisualElement. " +
                    "Ensure its PanelSettings and visualTreeAsset are configured before using the picker.");
            }

            var styleSheet = Resources.Load<StyleSheet>(k_UssResourcesPath);
            if (styleSheet == null)
            {
                throw new InvalidOperationException(
                    $"Resources/{k_UssResourcesPath}.uss not found in the SDK package. " +
                    "Package install may be incomplete.");
            }

            BuildHierarchy(hostRoot, styleSheet);

            m_CloseX!.clicked += () => { m_Overlay!.AddToClassList(k_OverlayHidden); _ = m_VM!.CompleteChoice(null); };
            m_Overlay!.AddToClassList(k_OverlayHidden);
            m_Initialized = true;
        }
    }
}

#endif
