#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEngine.Purchasing
{
    // UGUI implementation of IPaymentOptionProvider. View surface only — all state,
    // validation, and routing live in PurchaseOptionViewModel. Builds its Canvas hierarchy
    // lazily on first use; plain C# class on purpose so non-MonoBehaviour hosts (ECS, etc.)
    // can own it. Constructed via IPaymentProvidersStoreExtendedService.GetPaymentOptionProviderUGUI().
    internal sealed partial class PaymentOptionProvider : IPaymentOptionProvider
    {
        PurchaseOptionViewModel? m_VM;
        readonly IPurchaseEventEmitter? m_Emitter;

        GameObject? m_Root;
        CanvasGroup? m_Overlay;
        Transform? m_Sheet;
        Button? m_CloseX;

        Image? m_SheetImage;
        Text? m_TitleLabel;
        Image? m_CloseBgImage;
        Text? m_CloseGlyphLabel;

        bool m_Initialized;
        bool m_Disposed;

        readonly List<Button> m_DynamicButtons = new();

        internal PaymentOptionProvider(IPurchaseEventEmitter? emitter = null)
        {
            m_VM = new PurchaseOptionViewModel();
            m_Emitter = emitter;
        }

        void IPaymentOptionProvider.ApplyDarkTheme() => ApplyTheme(s_DarkTheme);

        void IPaymentOptionProvider.ApplyLightTheme() => ApplyTheme(s_LightTheme);

        void ApplyTheme(Theme theme)
        {
            EnsureInitialized();
            m_CurrentTheme = theme;
            if (m_SheetImage != null) m_SheetImage.color = theme.SheetBg;
            if (m_TitleLabel != null) m_TitleLabel.color = theme.TitleText;
            if (m_CloseGlyphLabel != null) m_CloseGlyphLabel.color = theme.CloseGlyph;
            // Close bg is always transparent in the new design.
            // Rebuild buttons so brand sprites swap to the new tone and bg colors update.
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
                SetVisible(true);
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
                SetVisible(true);
                EmitOptionsShown(opts);
            }
            var chosen = await task;
            ImpressionIdContext.Clear();
            return chosen;
        }

        // Maps the modal's internal PurchaseOpt list to the proto-faithful
        // PaymentOption set and forwards via the injected emitter. The emitter
        // mints the impression_id (ImpressionIdContext) so the subsequent
        // PurchaseIntentStartEvent for the chosen option carries the same id.
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
                // Unknown brand: skip rather than emit Unspecified — consumers count elements.
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
            if (m_Root is not null)
            {
                UnityEngine.Object.Destroy(m_Root);
                m_Root = null;
            }
            m_Overlay = null;
            m_Sheet = null;
            m_CloseX = null;
            m_SheetImage = null;
            m_TitleLabel = null;
            m_CloseBgImage = null;
            m_CloseGlyphLabel = null;
            m_Initialized = false;
            m_Disposed = true;
        }

        void EnsureInitialized()
        {
            if (m_Disposed) throw new InvalidOperationException("PaymentOptionProvider has been disposed");
            if (m_Initialized) return;

            BuildHierarchy();
            EnsureEventSystem();
            m_CloseX!.onClick.AddListener(() => { SetVisible(false); _ = m_VM!.CompleteChoice(null); });

            SetVisible(false);
            m_Initialized = true;
        }

        void SetVisible(bool visible)
        {
            if (m_Overlay == null)
                return;
            m_Overlay.alpha = visible ? 1f : 0f;
            m_Overlay.interactable = visible;
            m_Overlay.blocksRaycasts = visible;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(es);
        }

    }
}
