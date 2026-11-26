#if IAP_UIELEMENTS
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace UnityEngine.Purchasing
{
    // UI Toolkit implementation of IPaymentOptionProvider. View surface only — all state,
    // validation, and routing live in PurchaseOptionViewModel. Builds its own UIDocument and
    // runtime PanelSettings lazily on first use (see BuildPanel), the UITK counterpart of the
    // UGUI provider's own overlay Canvas, so its pt-based scale and sorting order don't
    // depend on — or interfere with — the game's panels. Constructed via
    // IPaymentProvidersExtendedService.GetPaymentOptionProviderUITK().
    internal sealed partial class PaymentOptionProviderUITK : IPaymentOptionProvider
    {
        // Never nulled: after Dispose the ViewModel refuses new flows and ignores late
        // CompleteChoice calls, which is what late callbacks need.
        readonly PurchaseOptionViewModel m_VM;
        readonly IPurchaseEventEmitter? m_Emitter;

        GameObject? m_Root;
        UIDocument? m_Document;
        PanelSettings? m_PanelSettings;

        VisualElement? m_Modal;
        VisualElement? m_Overlay;
        VisualElement? m_Sheet;
        VisualElement? m_ButtonBox;
        Button? m_CloseX;
        VisualElement? m_CloseGlyph;
        Label? m_Title;

        bool m_Initialized;
        bool m_Disposed;
        bool m_DarkMode;
        // The layout the consumer pinned (Apply*Layout): centered or not, or null for the
        // automatic one (the default, PaymentOptionPopMetrics.AutomaticCentered). Tests set it
        // before first use to pin a layout without initializing.
        internal bool? CenteredOverride { get; set; }
        // The layout applied, set in UpdatePanelScale.
        bool m_Centered;

        readonly List<Button> m_DynamicButtons = new();

        // viewModel: test seam, e.g. one whose eligibility request stays pending.
        internal PaymentOptionProviderUITK(IPurchaseEventEmitter? emitter = null, PurchaseOptionViewModel? viewModel = null)
        {
            m_VM = viewModel ?? new PurchaseOptionViewModel();
            m_Emitter = emitter;
        }

        // Test seams, read when the panel is built (first use). TargetTexture points the
        // POP's own panel at a RenderTexture instead of the screen, which is what lets the
        // capture tests see it; the scale is then resolved against the texture's size.
        // DpiOverride stands in for Screen.dpi, which is the desktop monitor's in the Editor,
        // and PlatformOverride for Application.platform, so a capture can take a device's
        // point scale (Apple's native scale differs from dpi/160) and its automatic layout,
        // with DeviceModelOverride for SystemInfo.deviceModel (iPhone vs iPad).
        internal RenderTexture? TargetTexture { get; set; }
        internal float? DpiOverride { get; set; }
        internal RuntimePlatform? PlatformOverride { get; set; }
        internal string? DeviceModelOverride { get; set; }
        RuntimePlatform Platform => PlatformOverride ?? Application.platform;
        string DeviceModel => DeviceModelOverride ?? SystemInfo.deviceModel;
        // Stands in for Screen.safeArea: in the target's pixels, origin bottom-left, like
        // Screen.safeArea. Read on every scale update rather than once, so a test can move it
        // (a rotation) without resizing. Without it, a panel on a TargetTexture has no insets.
        internal Rect? SafeAreaOverride { get; set; }

        // For tests: the POP's own panel and the root everything hangs under.
        internal PanelSettings? PanelSettings => m_PanelSettings;
        internal GameObject? PanelGameObject => m_Root;
        // Runs between building the panel and wiring the modal; lets a test make
        // initialization fail part-way.
        internal Action? AfterPanelBuiltForTests { get; set; }
        internal VisualElement? Root => m_Document != null ? m_Document.rootVisualElement : null;

        // For tests: shows these buttons without a pending choice, so a test can render
        // options only the ViewModel's listing flow composes (the webshop button) without
        // the stores behind that flow.
        internal void ShowButtonsForTests(IReadOnlyList<PurchaseOptionViewModel.PurchaseOpt> opts)
        {
            EnsureInitialized();
            BuildButtons(opts);
            m_Overlay?.RemoveFromClassList(k_OverlayHidden);
        }

        void IPaymentOptionProvider.ApplyDarkTheme()
        {
            EnsureInitialized();
            m_DarkMode = true;
            m_Modal?.AddToClassList(k_BlockDark);
            m_CloseGlyph?.MarkDirtyRepaint();
            if (m_CurrentOpts != null && m_DynamicButtons.Count > 0)
                BuildButtons(m_CurrentOpts);
        }

        void IPaymentOptionProvider.ApplyLightTheme()
        {
            EnsureInitialized();
            m_DarkMode = false;
            m_Modal?.RemoveFromClassList(k_BlockDark);
            m_CloseGlyph?.MarkDirtyRepaint();
            if (m_CurrentOpts != null && m_DynamicButtons.Count > 0)
                BuildButtons(m_CurrentOpts);
        }

        void IPaymentOptionProvider.SetLocale(string? localeTag)
        {
            if (m_Disposed) return;
            m_VM.SetLocale(localeTag);
            ApplyStrings();
            if (m_CurrentOpts != null && m_DynamicButtons.Count > 0)
                BuildButtons(m_CurrentOpts);
        }

        // Position and width rule. The bottom margin feeds the shrink-to-fit height, so
        // the panel scale is re-resolved along with the class; whether "Continue with" fits
        // depends on the width rule, so the buttons are refitted too.
        void IPaymentOptionProvider.ApplyBottomAnchoredLayout()
        {
            EnsureInitialized();
            ApplyLayout(false);
        }

        void IPaymentOptionProvider.ApplyCenteredLayout()
        {
            EnsureInitialized();
            ApplyLayout(true);
        }

        void IPaymentOptionProvider.ApplyAutomaticLayout()
        {
            EnsureInitialized();
            ApplyLayout(null);
        }

        void ApplyLayout(bool? centered)
        {
            CenteredOverride = centered;
            UpdatePanelScale();
            FitButtons();
        }

        Task<bool> IPaymentOptionProvider.ValidatePurchaseOption(string catalogListingId)
        {
            EnsureInitialized();
            return m_VM.ValidatePurchaseOption(catalogListingId);
        }

        bool IPaymentOptionProvider.ValidatePurchaseOption(IReadOnlyList<PurchaseOption> options)
        {
            EnsureInitialized();
            return m_VM.ValidatePurchaseOption(options);
        }

        async Task<string?> IPaymentOptionProvider.ShowPurchaseOption(string catalogListingId)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(catalogListingId))
                throw new ArgumentException("catalogListingId required", nameof(catalogListingId));

            var storeOptions = await m_VM.BuildDefaultOptions(catalogListingId);
            // Disposed during the eligibility request (scene change, teardown): documented as null.
            if (m_Disposed)
                return null;
            if (storeOptions.Count == 0 && !m_VM.HasWebshop(catalogListingId))
            {
                Debug.unityLogger.LogIAPWarning(
                    $"NoEligibleStore: no store has {catalogListingId} cached and no webshop available; returning null");
                return null;
            }
            // The panel may have been destroyed during the await: rebuild it (or throw) rather
            // than open a dead one, which would leave the caller's await pending.
            EnsureInitialized();
            // Each window is its own journey: Enter() always mints a new impression_id
            // (EnterOrJoin() would join an enclosing journey's instead). The view model
            // keeps it for the pick.
            using (ImpressionScope.Shared.Enter())
            {
                var task = m_VM.BeginShowPurchaseOptionForListing(storeOptions, catalogListingId, ImpressionScope.Shared.Current, out var opts);
                if (opts.Count > 0)
                {
                    OpenWindow(opts);
                }
                var chosen = await task;
                return chosen?.StoreName;
            }
        }

        async Task<PurchaseOption?> IPaymentOptionProvider.ShowPurchaseOption(IReadOnlyList<PurchaseOption> options)
        {
            EnsureInitialized();
            // See ShowPurchaseOption(string): one new impression_id per window.
            using (ImpressionScope.Shared.Enter())
            {
                var task = m_VM.BeginShowPurchaseOption(options, ImpressionScope.Shared.Current, out var opts);
                if (opts.Count > 0)
                {
                    OpenWindow(opts);
                }
                return await task;
            }
        }

        // Both ShowPurchaseOption overloads open the window here.
        void OpenWindow(IReadOnlyList<PurchaseOptionViewModel.PurchaseOpt> opts)
        {
            if (m_Overlay == null)
            {
                // No panel to show: resolve as cancelled so the caller's await returns null
                // and the next Show isn't refused as "already in progress".
                Debug.unityLogger.LogIAPWarning("Purchase Options UI has no panel; returning null");
                m_VM.CancelPendingChoice();
                return;
            }
            BuildButtons(opts);
            m_Overlay.RemoveFromClassList(k_OverlayHidden);
            m_CloseGlyph?.MarkDirtyRepaint();
            EmitOptionsShown(opts);
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
            m_VM.Dispose();
            m_CurrentOpts = null;
            DestroyPanel();
            m_Disposed = true;
        }

        // Destroying the GameObject takes the UIDocument, its panel and every element
        // with it. The PanelSettings is a copy of the template that nothing else owns.
        // Unity's null checks: either may already be gone (play-mode exit, or a scene
        // tool destroying the GameObject), and Destroy on those is skipped.
        void DestroyPanel()
        {
            ClearDynamicButtons();
            if (m_Root != null)
                Object.Destroy(m_Root);
            if (m_PanelSettings != null)
                Object.Destroy(m_PanelSettings);
            m_Root = null;
            m_PanelSettings = null;
            m_Document = null;
            m_Modal = null;
            m_Overlay = null;
            m_Sheet = null;
            m_ButtonBox = null;
            m_CloseX = null;
            m_Title = null;
            m_CloseGlyph = null;
            m_Initialized = false;
        }

        void EnsureInitialized()
        {
            if (m_Disposed) throw new InvalidOperationException("PaymentOptionProviderUITK has been disposed");
            if (m_Initialized)
            {
                if (m_Root != null) return;
                // The GameObject was destroyed without Dispose (play-mode exit, a scene
                // tool, a game "destroy everything" pass). Rebuild rather than keep
                // driving a dead panel, which would show nothing and resolve nothing.
                // The detach callback below has normally cancelled a pending choice
                // already; this covers a panel that never got to send it.
                m_VM.CancelPendingChoice();
                DestroyPanel();
            }

            var styleSheet = Resources.Load<StyleSheet>(k_UssResourcesPath);
            if (styleSheet == null)
            {
                throw new InvalidOperationException(
                    $"Resources/{k_UssResourcesPath}.uss not found in the SDK package. " +
                    "Package install may be incomplete.");
            }

            var panelSettings = Resources.Load<PanelSettings>(k_PanelSettingsResourcesPath);
            if (panelSettings == null)
            {
                throw new InvalidOperationException(
                    $"Resources/{k_PanelSettingsResourcesPath}.asset not found in the SDK package. " +
                    "Package install may be incomplete.");
            }

            try
            {
                var root = BuildPanel(styleSheet, panelSettings);
                AfterPanelBuiltForTests?.Invoke();
                BuildHierarchy(root);

                // The layout class and the overlay's margins, inset by the safe area (BuildPanel
                // ran before the overlay existed).
                UpdatePanelScale();
            }
            catch
            {
                // Don't leave a half-built panel (a full-screen GameObject and a
                // PanelSettings copy) behind; the next call retries from scratch.
                DestroyPanel();
                throw;
            }
            m_Initialized = true;
        }

        // Dismissal with no pick, from either the cancel button or a tap on the
        // dimmed backdrop. Both resolve the pending Task with null, which callers
        // already treat as "user cancelled".
        void Cancel()
        {
            // Defensive: in tests, a disposed UITK panel delivers no clicks even without this guard.
            if (m_Disposed) return;
            m_Overlay?.AddToClassList(k_OverlayHidden);
            _ = m_VM.CompleteChoice(null);
        }
    }
}

#endif
