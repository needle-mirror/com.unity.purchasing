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
        // Never nulled: after Dispose the ViewModel refuses new flows and ignores late
        // CompleteChoice calls, which is what late callbacks need.
        readonly PurchaseOptionViewModel m_VM;
        readonly IPurchaseEventEmitter? m_Emitter;

        GameObject? m_Root;
        Canvas? m_Canvas;
        CanvasScaler? m_Scaler;
        Vector2Int m_LastScreenSize;
        CanvasGroup? m_Overlay;
        Transform? m_ButtonBox;
        Button? m_CancelButton;
        TapCatcher? m_OutsideDismissCatcher;

        VerticalLayoutGroup? m_OverlayLayout;
        RectTransform? m_SafeAreaRect;
        Image? m_OverlayImage;
        LayoutElement? m_PopLayoutElement;
        RectTransform? m_HeaderRow;
        Image? m_PopBorderImage;
        Image? m_PopBackgroundImage;
        Text? m_HeaderTextLabel;
        readonly Image?[] m_CancelCrossStrokes = new Image?[2];

        bool m_Initialized;
        bool m_Disposed;

        readonly List<Button> m_DynamicButtons = new();

        // viewModel: test seam, e.g. one whose eligibility request stays pending.
        internal PaymentOptionProvider(IPurchaseEventEmitter? emitter = null, PurchaseOptionViewModel? viewModel = null)
        {
            m_VM = viewModel ?? new PurchaseOptionViewModel();
            m_Emitter = emitter;
        }

        // Test seams (internal), the counterpart of PaymentOptionProviderUITK's. Set before first
        // use. TargetTexture draws the canvas into a RenderTexture instead of the screen: a Screen
        // Space - Overlay canvas can't be captured, so the canvas is put in Screen Space - Camera
        // with a camera of its own (CaptureCamera), and the scale is resolved against the
        // texture's size. DpiOverride / PlatformOverride replace Screen.dpi / Application.platform
        // for the scale and the automatic layout, DeviceModelOverride SystemInfo.deviceModel for the
        // latter; SafeAreaOverride replaces Screen.safeArea (pixels, origin bottom-left).
        // Used by PaymentOptionProviderUGUICaptureTests and PaymentOptionProviderUGUITests.
        internal RenderTexture? TargetTexture { get; set; }
        internal float? DpiOverride { get; set; }
        internal RuntimePlatform? PlatformOverride { get; set; }
        internal string? DeviceModelOverride { get; set; }
        internal Rect? SafeAreaOverride { get; set; }
        internal Camera? CaptureCamera => m_CaptureCamera;
        Camera? m_CaptureCamera;

        // For tests: the scrim color and the POP's preferred width in pt (the overlay
        // layout narrows it to the space between the side margins).
        internal Color? ScrimColor => m_OverlayImage != null ? m_OverlayImage.color : (Color?)null;
        // For tests: the tint the scrim and the POP's border are drawn with. A Selectable's state
        // tint lands on the CanvasRenderer, not on Image.color, so ScrimColor alone can't see it.
        internal Color? ScrimRendererTint => m_OverlayImage != null ? m_OverlayImage.canvasRenderer.GetColor() : (Color?)null;
        internal Color? PopBorderRendererTint => m_PopBorderImage != null ? m_PopBorderImage.canvasRenderer.GetColor() : (Color?)null;
        // For tests: the dimmed backdrop, to tap.
        internal GameObject? Backdrop => m_OverlayImage != null ? m_OverlayImage.gameObject : null;
        internal float? PopPreferredWidth => m_PopLayoutElement != null ? m_PopLayoutElement.preferredWidth : (float?)null;
        internal float? CanvasScale => m_Canvas != null ? m_Canvas.scaleFactor : (float?)null;
        internal RectTransform? PopRect => m_PopLayoutElement != null ? (RectTransform)m_PopLayoutElement.transform : null;
        internal IReadOnlyList<Button> OptionButtons => m_DynamicButtons;
        internal Button? CancelButton => m_CancelButton;

        // For tests: shows these buttons without a pending choice, so a test can render options
        // only the ViewModel's listing flow composes (the webshop button) without the stores
        // behind that flow.
        internal void ShowButtonsForTests(IReadOnlyList<PurchaseOptionViewModel.PurchaseOpt> opts)
        {
            EnsureInitialized();
            BuildButtons(opts);
            SetVisible(true);
        }

        // For tests: an option button's background and the logo after its "Continue with", if any.
        internal (Color background, Sprite? logo)? OptionButtonVisuals(string storeName)
        {
            foreach (var b in m_DynamicButtons)
            {
                if (b == null || b.name != $"Pay_{storeName}")
                    continue;
                var logo = b.transform.Find("Content/Logo");
                return (b.GetComponent<Image>().color, logo != null ? logo.GetComponent<Image>().sprite : null);
            }
            return null;
        }

        // For tests: the cancel cross's strokes.
        internal IReadOnlyList<Image?> CancelCrossStrokes => m_CancelCrossStrokes;
        // For tests: the safe area the POP is laid out in.
        internal RectTransform? SafeAreaRect => m_SafeAreaRect;

        void IPaymentOptionProvider.ApplyDarkTheme() => ApplyTheme(s_DarkTheme);

        void IPaymentOptionProvider.ApplyLightTheme() => ApplyTheme(s_LightTheme);

        void IPaymentOptionProvider.SetLocale(string? localeTag)
        {
            if (m_Disposed) return;
            m_VM.SetLocale(localeTag);
            ApplyStrings();
            if (m_CurrentOpts != null && m_DynamicButtons.Count > 0)
                BuildButtons(m_CurrentOpts);
        }

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

        void ApplyTheme(Theme theme)
        {
            EnsureInitialized();
            m_CurrentTheme = theme;
            if (m_PopBackgroundImage != null) m_PopBackgroundImage.color = theme.Colors.PopBackground;
            if (m_PopBorderImage != null) m_PopBorderImage.color = theme.Colors.PopBorder;
            if (m_HeaderTextLabel != null) m_HeaderTextLabel.color = theme.Colors.HeaderText;
            foreach (var stroke in m_CancelCrossStrokes)
                if (stroke != null) stroke.color = theme.Colors.CancelButton;
            // Cancel button bg is always transparent in the new design.
            // Rebuild buttons so brand sprites swap to the new tone and bg colors update.
            if (m_CurrentOpts != null && m_DynamicButtons.Count > 0)
                BuildButtons(m_CurrentOpts);
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
            BuildButtons(opts);
            SetVisible(true);
            EmitOptionsShown(opts);
        }

        // Maps the modal's internal PurchaseOpt list to the proto-faithful
        // PaymentOption set and forwards via the injected emitter, which stamps
        // the window's impression_id from the current scope.
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
            // Static event — leaving this subscribed would keep the whole provider,
            // and the destroyed GameObjects it references, alive for the process.
            // Unconditional: unsubscribing when never subscribed is a no-op, and
            // BuildHierarchy throwing part-way would otherwise leak the handler.
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            m_VM.Dispose();
            ClearDynamicButtons();
            m_CurrentOpts = null;
            if (m_Root is not null)
            {
                UnityEngine.Object.Destroy(m_Root);
                m_Root = null;
            }
            if (m_CaptureCamera is not null)
            {
                UnityEngine.Object.Destroy(m_CaptureCamera.gameObject);
                m_CaptureCamera = null;
            }
            m_Canvas = null;
            m_Scaler = null;
            m_Overlay = null;
            m_ButtonBox = null;
            m_CancelButton = null;
            m_OutsideDismissCatcher = null;
            m_OverlayLayout = null;
            m_SafeAreaRect = null;
            m_OverlayImage = null;
            m_PopLayoutElement = null;
            m_HeaderRow = null;
            m_PopBorderImage = null;
            m_PopBackgroundImage = null;
            m_HeaderTextLabel = null;
            m_CancelCrossStrokes[0] = m_CancelCrossStrokes[1] = null;
            m_Initialized = false;
            m_Disposed = true;
        }

        void EnsureInitialized()
        {
            if (m_Disposed) throw new InvalidOperationException("PaymentOptionProvider has been disposed");
            if (m_Initialized) return;

            BuildHierarchy();
            EnsureEventSystem();

            SetVisible(false);
            m_Initialized = true;
        }

        // Dismissal with no pick, from either the cancel button or a tap on the
        // dimmed backdrop. Both resolve the pending Task with null, which callers
        // already treat as "user cancelled".
        void Cancel()
        {
            // Destroy is deferred to the end of the frame, so a click can still arrive after Dispose.
            if (m_Disposed) return;
            SetVisible(false);
            _ = m_VM.CompleteChoice(null);
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
            var es = UIEventSystemUtility.CreateEventSystem();
            UnityEngine.Object.DontDestroyOnLoad(es);
        }

    }
}
