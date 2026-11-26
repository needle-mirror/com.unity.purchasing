#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.UI;
#if IAP_UGUI_TMP
using TMPro;
#endif

namespace UnityEngine.Purchasing
{
    /// UGUI hierarchy + procedural sprite generation, partialed out so the
    /// behavioural surface (Init / Show / Resolve / Theme / Dispose) stays
    /// readable in PaymentOptionProvider.cs. Runtime-only — no AssetDatabase
    /// or EncodeToPNG.
    internal sealed partial class PaymentOptionProvider
    {
        // === Simple POP spec values ===
        // The canvas is ConstantPixelSize with a dpi-derived scaleFactor (see
        // PaymentOptionPopMetrics.PointScale), so one canvas unit is one pt — iOS
        // point / Android dp — and every number below is the spec's number verbatim.
        // Don't convert them. They live in PaymentOptionPopMetrics, shared with the UITK
        // provider; they are aliased here so this file reads the same as before the move.
        const float c_PopSideMargin    = PaymentOptionPopMetrics.SideMargin;
        const float c_PopMinWidth      = PaymentOptionPopMetrics.MinWidth;
        const float c_PopPadding       = PaymentOptionPopMetrics.Padding;
        const float c_PopCorner        = PaymentOptionPopMetrics.CornerRadius;
        const float c_PopBorderWidth   = PaymentOptionPopMetrics.BorderWidth;
        const float c_HeaderLineHeight = PaymentOptionPopMetrics.HeaderLineHeight;
        const int   c_HeaderFontSize   = PaymentOptionPopMetrics.HeaderFontSize;
        const float c_HeaderGap        = PaymentOptionPopMetrics.HeaderGap;
        const float c_ButtonHeight     = PaymentOptionPopMetrics.ButtonHeight;
        const float c_ButtonCorner     = PaymentOptionPopMetrics.ButtonCornerRadius;
        const float c_ButtonGap        = PaymentOptionPopMetrics.ButtonGap;
        const int   c_ButtonFontSize   = PaymentOptionPopMetrics.ButtonFontSize;
        const float c_ButtonSidePadding = PaymentOptionPopMetrics.ButtonSidePadding;

        // Not in the Simple POP spec. The visible button is 32pt and drives the header
        // row height; the hit target is a transparent 32->44pt child that overflows it,
        // because 44pt is the platform minimum touch target. Taps on the overflow land
        // on the child, and UGUI dispatches to the first ancestor that handles clicks —
        // the cancel Button — so the row stays 32pt without shrinking the tap area.
        const float c_CancelButtonSize = PaymentOptionPopMetrics.CancelButtonSize;
        const float c_CancelHitSize    = PaymentOptionPopMetrics.CancelHitSize;
        // The cross: two round-capped strokes corner to corner of a 16pt box, as on UITK.
        const float c_CancelCrossSize  = PaymentOptionPopMetrics.CancelGlyphSize;
        internal const float k_CancelCrossStroke = 2f;
        // The header text -> cancel button gap.
        const float c_HeaderSpacing    = 8f;

        // The height of a button label's rect. The text is centered in it and doesn't wrap,
        // so it only has to be taller than a line.
        const float c_LabelHeight = 24f;

        // The pt -> pixel factor: one canvas unit per pt, shrunk to fit small screens and the
        // safe area. The arithmetic is shared with the UITK provider (PaymentOptionPopMetrics);
        // this supplies the real screen metrics and the current content. Vertically it reserves
        // the bottom-anchored 48pt margin, or centered 16pt above and below (ReservedHeight).
        float ResolveScaleFactor()
        {
            var screen = ScreenPixels();
            var safe = SafeAreaPixels(screen);
            return PaymentOptionPopMetrics.ResolveScaleFactor(DpiOverride ?? Screen.dpi,
                Platform,
                Mathf.FloorToInt(safe.width), Mathf.FloorToInt(safe.height),
                m_CurrentOpts?.Count ?? 0, PaymentOptionPopMetrics.ReservedHeight(m_CurrentLayout.Centered));
        }

        // The screen's (or the test target texture's) size in pixels.
        Vector2Int ScreenPixels()
            => TargetTexture != null
                ? new Vector2Int(TargetTexture.width, TargetTexture.height)
                : new Vector2Int(Screen.width, Screen.height);

        // Screen.safeArea, or the test's stand-in, kept within the screen. A canvas drawn to a
        // texture isn't on the screen, so the screen's safe area doesn't apply to it.
        Rect SafeAreaPixels(Vector2Int screen)
        {
            var area = SafeAreaOverride
                ?? (TargetTexture != null ? new Rect(0f, 0f, screen.x, screen.y) : Screen.safeArea);
            return PaymentOptionPopMetrics.ClampSafeArea(area, screen.x, screen.y);
        }

        // Applied to the Canvas as well as the CanvasScaler: the scaler only pushes
        // its value in its own Update, so without this the force-rebuild in
        // SettlePopLayout would measure against the previous frame's scale.
        // Also moves the safe area's edges, in pt at the new scale.
        void UpdateCanvasScale()
        {
            if (m_Scaler == null || m_Canvas == null) return;
            // First: the layout mode decides the reserved height the scale is resolved for.
            UpdateLayoutMode();
            var scale = ResolveScaleFactor();
            m_Scaler.scaleFactor = scale;
            m_Canvas.scaleFactor = scale;

            var screen = ScreenPixels();
            var safe = SafeAreaPixels(screen);
            m_AppliedSafeArea = safe;
            PaymentOptionPopMetrics.SafeAreaInsets(safe, screen.x, screen.y, scale,
                out m_SafeLeft, out m_SafeRight, out m_SafeTop, out m_SafeBottom);
            if (m_SafeAreaRect != null)
            {
                m_SafeAreaRect.offsetMin = new Vector2(m_SafeLeft, m_SafeBottom);
                m_SafeAreaRect.offsetMax = new Vector2(-m_SafeRight, -m_SafeTop);
            }
        }

        Rect m_AppliedSafeArea;
        // The safe area's insets in pt, as last applied.
        float m_SafeLeft, m_SafeRight, m_SafeTop, m_SafeBottom;

        // Screen size and safe area changes have no event, and the provider is a plain C#
        // class with no Update of its own, so it rides the canvas render loop and compares.
        // The safe area is compared too: turning a phone from one landscape side to the
        // other moves the notch without resizing anything.
        // Only the scale, the safe area and the buttons' content (FitButtons) are touched
        // here — forcing a layout rebuild during willRenderCanvases re-enters the rebuild
        // loop Unity is already running. Changing the canvas scale dirties the layout, which
        // settles on the next pass: CanvasUpdateRegistry subscribes to willRenderCanvases
        // long before this does, and handlers run in subscription order, so that frame's
        // layout has already been performed by the time this runs. A resize or rotation
        // therefore renders one frame at the previous scale.
        void OnWillRenderCanvases()
        {
            var size = ScreenPixels();
            if (size == m_LastScreenSize && SafeAreaPixels(size) == m_AppliedSafeArea) return;
            m_LastScreenSize = size;
            UpdateCanvasScale();
            FitButtons();
        }

        // Bottom-anchored vs dead-centered follows the device until the SDK consumer pins
        // one with ApplyBottomAnchoredLayout / ApplyCenteredLayout (ApplyAutomaticLayout
        // goes back): PaymentOptionPopMetrics.AutomaticCentered, from the screen's size and the
        // device (UpdateLayoutMode).
        readonly struct LayoutMode
        {
            public readonly TextAnchor Alignment;
            // Centered: dead center, 375pt wide, the preferred width following the content
            // up to PaymentOptionPopMetrics.CenteredMaxWidth (see FitButtons), 16pt kept above
            // and below. Otherwise bottom-anchored, 48pt above the bottom, up to 412pt wide.
            public readonly bool Centered;

            public LayoutMode(TextAnchor alignment, bool centered)
            {
                Alignment = alignment;
                Centered = centered;
            }
        }

        // Mobile: fills the width between the side margins, capped at 412pt.
        static readonly LayoutMode s_BottomAnchoredLayout = new LayoutMode(TextAnchor.LowerCenter, centered: false);

        // Tablet: dead center, 375pt wide, growing with wider content up to 500pt.
        static readonly LayoutMode s_CenteredLayout = new LayoutMode(TextAnchor.MiddleCenter, centered: true);

        // The layout the consumer pinned (Apply*Layout), centered or not, or null for the
        // automatic one (the default). Tests set it before first use to pin a layout without
        // initializing; set later, it applies on the next layout update.
        internal bool? CenteredOverride { get; set; }
        // What is applied: the override, or the device's automatic layout.
        LayoutMode m_CurrentLayout = s_BottomAnchoredLayout;

        RuntimePlatform Platform => PlatformOverride ?? Application.platform;
        string DeviceModel => DeviceModelOverride ?? SystemInfo.deviceModel;

        // A spec palette (PaymentOptionPopPalette, shared with the UITK provider). The option
        // buttons take their colors and logo tones from it by button kind.
        readonly struct Theme
        {
            public readonly bool Dark;
            public readonly PaymentOptionPopPalette.Colors Colors;

            public Theme(bool dark)
            {
                Dark = dark;
                Colors = PaymentOptionPopPalette.For(dark);
            }
        }

        static readonly Theme s_LightTheme = new Theme(dark: false);
        static readonly Theme s_DarkTheme = new Theme(dark: true);

        Theme m_CurrentTheme = s_LightTheme;
        IReadOnlyList<PurchaseOptionViewModel.PurchaseOpt>? m_CurrentOpts;

        // === Procedural sprite caches (cheap; outlive provider instances) ===
        // Unity's null check rather than ??=: exiting Play Mode destroys these sprites, and without a
        // domain reload ??= would keep returning the destroyed ones.
        static Sprite? s_PopSprite;
        static Sprite? s_ButtonSprite;
        static Sprite? s_BadgeSprite;
        static Sprite? s_StrokeSprite;
        static Sprite PopSprite  => s_PopSprite  != null ? s_PopSprite  : s_PopSprite  = CreateRoundedSprite(c_PopCorner);
        static Sprite ButtonSprite => s_ButtonSprite != null ? s_ButtonSprite : s_ButtonSprite = CreateRoundedSprite(c_ButtonCorner);
        static Sprite BadgeSprite  => s_BadgeSprite  != null ? s_BadgeSprite  : s_BadgeSprite  = CreateRoundedSprite(PaymentOptionPopMetrics.BadgeCornerRadius);
        // Fully round ends on a stroke as thick as twice its radius: the cancel cross's round caps.
        static Sprite StrokeSprite => s_StrokeSprite != null ? s_StrokeSprite : s_StrokeSprite = CreateRoundedSprite(k_CancelCrossStroke / 2f);

        // Consumer-visible, do not rename: documented as the way to reach the Canvas
        // (e.g. to raise its sortingOrder), so projects look it up by this name.
        internal const string k_GameObjectName = "PaymentOptionProvider";

        void BuildHierarchy()
        {
            m_Root = new GameObject(k_GameObjectName,
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Object.DontDestroyOnLoad(m_Root);

            m_Canvas = m_Root.GetComponent<Canvas>();
            var canvas = m_Canvas;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (TargetTexture != null)
                RenderToTargetTexture(canvas, TargetTexture);
            // Sit above typical game/HUD canvases (0–1000) with plenty of headroom.
            // Projects whose UI layers higher can override by grabbing the
            // "PaymentOptionProvider" GameObject's Canvas after first Show.
            canvas.sortingOrder = 10000;

            // ConstantPixelSize with a dpi-derived factor makes one canvas unit one
            // pt, which is what keeps the modal at a consistent physical size across
            // DPIs. ScaleWithScreenSize would instead pin it to a fraction of screen
            // height, so the same modal is physically tiny on a phone and huge on a
            // tablet — and none of the spec's pt values would mean anything.
            m_Scaler = m_Root.GetComponent<CanvasScaler>();
            m_Scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var overlayGo = NewUiChild(m_Root.transform, "PurchaseOverlay",
                typeof(Image), typeof(CanvasGroup));
            StretchFull(overlayGo);
            m_OverlayImage = overlayGo.GetComponent<Image>();
            m_OverlayImage.color = PaymentOptionPopPalette.Scrim;
            m_Overlay = overlayGo.GetComponent<CanvasGroup>();
            m_Overlay.alpha = 0f;
            m_Overlay.interactable = false;
            m_Overlay.blocksRaycasts = false;

            // Tap-outside-to-dismiss. Taps on the POP itself are swallowed by the
            // catcher on the card (below), so only taps on the dimmed backdrop reach this.
            m_OutsideDismissCatcher = overlayGo.AddComponent<TapCatcher>();
            m_OutsideDismissCatcher.Tapped += Cancel;

            // The safe area (Screen.safeArea: notch, rounded corners, home indicator): the
            // scrim covers the whole screen, while the POP is laid out within this, so its
            // margins are measured from the safe area's edges and the centered POP is
            // centered in it. Its edges are set with the scale (UpdateCanvasScale). It has no
            // graphic, so taps on it fall through to the scrim's dismiss button.
            var safeGo = NewUiChild(overlayGo.transform, "SafeArea", typeof(VerticalLayoutGroup));
            StretchFull(safeGo);
            m_SafeAreaRect = (RectTransform)safeGo.transform;

            // Positions the POP (bottom-anchored or centered) and sizes it:
            // childControlWidth with flexibleWidth 0 clamps the width to
            // clamp(available, minWidth, preferredWidth), and childControlHeight with
            // childForceExpandHeight false sets the height to the POP's own preferred
            // height — so the POP needs no ContentSizeFitter, which Unity warns against
            // on a child of a layout group. ApplyLayout drives padding + alignment.
            m_OverlayLayout = safeGo.GetComponent<VerticalLayoutGroup>();
            m_OverlayLayout.childControlWidth = true;
            m_OverlayLayout.childControlHeight = true;
            m_OverlayLayout.childForceExpandWidth = false;
            m_OverlayLayout.childForceExpandHeight = false;

            m_LastScreenSize = ScreenPixels();
            UpdateCanvasScale();
            Canvas.willRenderCanvases += OnWillRenderCanvases;

            // The POP's own Image is the 1pt border; the inset Fill child below is
            // the background. Two sliced images is the cheapest way to get a
            // rounded-rect outline without shipping a second sprite per corner size.
            var popGo = NewUiChild(safeGo.transform, "Pop",
                typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            m_PopLayoutElement = popGo.GetComponent<LayoutElement>();
            m_PopLayoutElement.minWidth = c_PopMinWidth;
            m_PopLayoutElement.flexibleWidth = 0f;

            // Swallows taps that land on the POP so they never reach the backdrop's
            // dismiss handler. UGUI delivers a click to the first *ancestor* that
            // handles it, and an Image is not a handler — so without a handler here,
            // a tap on the POP's background, header or padding bubbles up to the
            // overlay and closes the modal. A listener-less TapCatcher is the handler;
            // the option buttons are handlers in their own right and are unaffected.
            popGo.AddComponent<TapCatcher>();

            m_PopBorderImage = popGo.GetComponent<Image>();
            m_PopBorderImage.color = m_CurrentTheme.Colors.PopBorder;
            m_PopBorderImage.sprite = PopSprite;
            m_PopBorderImage.type = Image.Type.Sliced;

            var fillGo = NewUiChild(popGo.transform, "Fill", typeof(Image), typeof(LayoutElement));
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(c_PopBorderWidth, c_PopBorderWidth);
            fillRt.offsetMax = new Vector2(-c_PopBorderWidth, -c_PopBorderWidth);
            // First sibling so it draws under the header and buttons; ignoreLayout so
            // the POP's VerticalLayoutGroup treats it as decoration, not content.
            fillGo.GetComponent<LayoutElement>().ignoreLayout = true;
            m_PopBackgroundImage = fillGo.GetComponent<Image>();
            m_PopBackgroundImage.color = m_CurrentTheme.Colors.PopBackground;
            m_PopBackgroundImage.sprite = PopSprite;
            m_PopBackgroundImage.type = Image.Type.Sliced;
            m_PopBackgroundImage.raycastTarget = false;

            var popLayout = popGo.GetComponent<VerticalLayoutGroup>();
            popLayout.padding = new RectOffset(
                (int)c_PopPadding, (int)c_PopPadding, (int)c_PopPadding, (int)c_PopPadding);
            popLayout.spacing = c_HeaderGap;
            popLayout.childAlignment = TextAnchor.UpperCenter;
            popLayout.childControlWidth = true;
            popLayout.childControlHeight = true;
            popLayout.childForceExpandWidth = true;
            popLayout.childForceExpandHeight = false;

            BuildHeaderRow(popGo.transform);
            BuildButtonBox(popGo.transform);
            ApplyLayout(CenteredOverride);
        }

        // Test seam (TargetTexture): Screen Space - Overlay always draws to the screen, so the
        // canvas is drawn by a camera of its own into the texture instead. The canvas then spans
        // the camera's pixel rect, the texture, exactly as an overlay canvas spans the screen.
        // Its own GameObject, not a child of the canvas, which a Screen Space - Camera canvas
        // moves in front of its camera.
        void RenderToTargetTexture(Canvas canvas, RenderTexture target)
        {
            var cameraGo = new GameObject(k_GameObjectName + "CaptureCamera", typeof(Camera));
            Object.DontDestroyOnLoad(cameraGo);
            m_CaptureCamera = cameraGo.GetComponent<Camera>();
            m_CaptureCamera.targetTexture = target;
            m_CaptureCamera.clearFlags = CameraClearFlags.SolidColor;
            // A mid gray stands in for the game behind the scrim.
            m_CaptureCamera.backgroundColor = new Color(0.55f, 0.6f, 0.65f, 1f);
            m_CaptureCamera.orthographic = true;
            m_CaptureCamera.nearClipPlane = 0.1f;
            m_CaptureCamera.farClipPlane = 100f;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = m_CaptureCamera;
            canvas.planeDistance = 10f;
        }

        // Catches taps on the scrim and on the POP, and nothing else: no visual state and no
        // selection. Not a Button: a Button tints its graphic for its state, and one added while
        // the overlay's CanvasGroup is non-interactable tints it Disabled at once (half alpha,
        // 78% gray) — which drew the scrim at about 30% instead of 60% and the POP border half
        // transparent — and Transition.None set afterwards doesn't undo a tint already applied.
        // Not a Selectable either, so a tap never takes the EventSystem's selection from the
        // game's own UI.
        internal sealed class TapCatcher : MonoBehaviour, EventSystems.IPointerClickHandler
        {
            internal event Action? Tapped;

            public void OnPointerClick(EventSystems.PointerEventData eventData) => Tapped?.Invoke();
        }

        // Bottom-anchored (mobile) vs dead-centered (tablet). Safe to call before or
        // after the first Show; the layout group re-runs on the next rebuild. The margins
        // are measured from the safe area's edges (m_OverlayLayout lays out within it):
        // 16pt each side, then 48pt from the bottom, or centered 16pt above and below.
        void ApplyLayout(bool? centered)
        {
            CenteredOverride = centered;
            SettlePopLayout();
        }

        // Applies the override, or the device's automatic layout for the current screen.
        // Doesn't rebuild: it also runs from OnWillRenderCanvases (via UpdateCanvasScale),
        // where the dirtied layout group settles on the next pass.
        void UpdateLayoutMode()
        {
            var screen = ScreenPixels();
            m_CurrentLayout = CenteredOverride ?? PaymentOptionPopMetrics.AutomaticCentered(Platform, DeviceModel, DpiOverride ?? Screen.dpi, screen.x, screen.y)
                ? s_CenteredLayout : s_BottomAnchoredLayout;
            if (m_OverlayLayout == null) return;
            var layout = m_CurrentLayout;
            var vertical = layout.Centered ? (int)PaymentOptionPopMetrics.CenteredVerticalMargin : 0;
            var bottom = layout.Centered ? vertical : (int)PaymentOptionPopMetrics.BottomAnchoredMargin;
            m_OverlayLayout.padding = new RectOffset(
                (int)c_PopSideMargin, (int)c_PopSideMargin, vertical, bottom);
            m_OverlayLayout.childAlignment = layout.Alignment;
        }

        // Groups the option buttons so their gap is independent of the
        // header -> buttons gap (one VerticalLayoutGroup can only express one
        // spacing). Reports its own preferred height to the POP layout, so no
        // ContentSizeFitter needed here.
        void BuildButtonBox(Transform pop)
        {
            var go = NewUiChild(pop, "ButtonBox", typeof(VerticalLayoutGroup));
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = c_ButtonGap;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            m_ButtonBox = go.transform;
        }

        void BuildHeaderRow(Transform pop)
        {
            var rowGo = NewUiChild(pop, "Header",
                typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            m_HeaderRow = (RectTransform)rowGo.transform;
            var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
            // No side padding — header text and cancel button align with the button box edges.
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = c_HeaderSpacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            // At least the cancel button rather than the 28pt text line; a header that wraps
            // grows it. The button's larger touch target overflows this row instead of growing it.
            var le = rowGo.GetComponent<LayoutElement>();
            le.minHeight = c_CancelButtonSize;

            m_HeaderTextLabel = AddText(rowGo.transform, "HeaderText", HeaderText(),
                fontSize: c_HeaderFontSize, bold: true, color: m_CurrentTheme.Colors.HeaderText,
                align: TextAnchor.MiddleLeft);
            var headerTextLe = m_HeaderTextLabel.gameObject.AddComponent<LayoutElement>();
            headerTextLe.flexibleWidth = 1f;
            // A minimum, so the Text's own preferred height takes over when it wraps.
            headerTextLe.minHeight = c_HeaderLineHeight;

            BuildCancelButton(rowGo.transform);
        }

        void BuildCancelButton(Transform parent)
        {
            var go = new GameObject("CancelButton",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, worldPositionStays: false);
            rt.sizeDelta = new Vector2(c_CancelButtonSize, c_CancelButtonSize);

            // Invisible background — the X is the only visible part. Still needed:
            // it is the Button's raycast target inside the 32pt box.
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = c_CancelButtonSize;
            le.preferredHeight = c_CancelButtonSize;

            m_CancelButton = go.GetComponent<Button>();
            m_CancelButton.onClick.AddListener(Cancel);

            BuildCancelHitArea(go.transform);
            BuildCancelCross(go.transform);
        }

        // Transparent graphic centred on the 32pt button and sized to the 44pt
        // platform minimum, so it overhangs by 6pt on each side. Nothing masks the
        // POP, so the overhang still raycasts; the click is dispatched to the cancel
        // Button above it. The 6pt reaches only the header's padding, the 8pt gap
        // before the (non-interactive) header text, and the 20pt gap above the first
        // option button — no other Button is within reach.
        static void BuildCancelHitArea(Transform cancelButton)
        {
            var go = NewUiChild(cancelButton, "HitArea", typeof(Image), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(c_CancelHitSize, c_CancelHitSize);
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            // The button has no layout group, but ignoreLayout keeps this out of the
            // parent's preferred size if one is ever added.
            go.GetComponent<LayoutElement>().ignoreLayout = true;
        }

        string CurrentLocale => m_VM.Locale;

        string HeaderText() => PaymentOptionProviderTranslations.Translate(PaymentOptionProviderTranslations.Keys.Header, CurrentLocale);

        /// Refreshes strings on the parts of the hierarchy that outlive a single show.
        /// The option buttons need nothing here — they are rebuilt from scratch below.
        void ApplyStrings()
        {
            if (m_HeaderTextLabel != null)
                m_HeaderTextLabel.text = HeaderText();
        }

        // Drawn rather than typed, as on UITK: the built-in font's "×" is a small, heavy
        // glyph, while the spec's cancel icon is a thin cross 16pt across. Two strokes 2pt
        // thick from corner to corner of the 16pt box, inset by half the stroke so their
        // round caps stay inside it: each is a pill (StrokeSprite, sliced) as long as the
        // diagonal between the cap centers plus one cap, turned 45 degrees either way.
        void BuildCancelCross(Transform cancelButton)
        {
            var boxGo = NewUiChild(cancelButton, "Cross", typeof(LayoutElement));
            var box = (RectTransform)boxGo.transform;
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.pivot = new Vector2(0.5f, 0.5f);
            box.anchoredPosition = Vector2.zero;
            box.sizeDelta = new Vector2(c_CancelCrossSize, c_CancelCrossSize);
            boxGo.GetComponent<LayoutElement>().ignoreLayout = true;

            var length = (c_CancelCrossSize - k_CancelCrossStroke) * Mathf.Sqrt(2f) + k_CancelCrossStroke;
            for (var i = 0; i < m_CancelCrossStrokes.Length; i++)
            {
                var strokeGo = NewUiChild(box, "Stroke", typeof(Image));
                var stroke = (RectTransform)strokeGo.transform;
                stroke.anchorMin = stroke.anchorMax = new Vector2(0.5f, 0.5f);
                stroke.pivot = new Vector2(0.5f, 0.5f);
                stroke.anchoredPosition = Vector2.zero;
                stroke.sizeDelta = new Vector2(length, k_CancelCrossStroke);
                stroke.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
                var img = strokeGo.GetComponent<Image>();
                img.sprite = StrokeSprite;
                img.type = Image.Type.Sliced;
                img.color = m_CurrentTheme.Colors.CancelButton;
                img.raycastTarget = false;
                m_CancelCrossStrokes[i] = img;
            }
        }

        void BuildButtons(IReadOnlyList<PurchaseOptionViewModel.PurchaseOpt> opts)
        {
            m_CurrentOpts = opts;
            ApplyStrings();
            ClearDynamicButtons();
            if (m_ButtonBox == null) return;

            foreach (var opt in opts)
            {
                var button = PurchaseOptionViewModel.DescribeButton(opt, CurrentLocale);
                var tone = PaymentOptionPopPalette.LogoTone(button.Kind, m_CurrentTheme.Dark);
                var brand = button.HasBrand ? PaymentBrandRegistry.GetBrand(opt.StoreName, tone) : null;
                if (button.HasBrand && brand == null)
                    button = button.WithoutBrand();
                m_DynamicButtons.Add(BuildOptionButton(opt, button, brand));
            }

            SettlePopLayout();
        }

        // One rebuild from the safe area down: it sizes and positions the POP in the
        // same walk, so there is no fitter to settle first. Called after the buttons
        // change so the POP never renders a frame at the wrong size or offset.
        void SettlePopLayout()
        {
            // Button count feeds PaymentOptionPopMetrics.RequiredPopHeight, so the scale
            // has to be refreshed whenever the content or the layout mode changes, not
            // just on resize.
            UpdateCanvasScale();
            // Sets the POP's preferred width and lays out each button's content, from the
            // measured texts and the screen: no layout pass needed first.
            FitButtons();
            // From the safe area, which holds the layout group: ForceRebuildLayoutImmediate only
            // applies layout below a root that is a layout controller itself, and the scrim isn't.
            if (m_SafeAreaRect == null)
                return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_SafeAreaRect);
        }

        // The parts of a button laid out in C# rather than by a layout group: its content
        // ("Continue with" and the logo, or the whole label) and its badge, positioned from
        // the measured widths per PaymentOptionPopMetrics, as the UITK provider's FitButton does.
        sealed class ButtonFit
        {
            public readonly RectTransform Button;
            public readonly RectTransform Content;
            // The whole label, or "Continue with" when there is a logo: a Text, or beside the
            // Apple lockup a TextMeshProUGUI where the system font's Medium face is available
            // (AppleSystemTmpFont). The Label* helpers below handle either.
            public readonly Graphic Label;
            public readonly string LabelText;
            public Image? Logo;
            public float LogoWidth;
            public float LogoHeight;
            public float LogoGap;
            // Where the logo's lettering sits on its line, as a fraction of the logo's height
            // above its center (negative: below), for the label to share its baseline; null to
            // center the label instead.
            public float? LogoBaseline;
            // Text after the logo, when the translation has any (pop.button's {0} not last).
            public Graphic? Trail;
            public string TrailText = "";
            public RectTransform? Badge;
            public Text? BadgeLabel;
            public string BadgeText = "";

            public ButtonFit(RectTransform button, RectTransform content, Graphic label, string labelText)
            {
                Button = button;
                Content = content;
                Label = label;
                LabelText = OneLine(labelText);
            }
        }

        readonly List<ButtonFit> m_ButtonFits = new();

        // What a row decided: the width it asks of a centered POP (its content and its badge's
        // reserve), and the width that keeps its content centered beside its badge (0: none).
        readonly struct RowFit
        {
            public readonly float Ask;
            public readonly float CenteredMinWidth;

            public RowFit(float ask, float centeredMinWidth)
            {
                Ask = ask;
                CenteredMinWidth = centeredMinWidth;
            }
        }

        // The POP's width and every button's content, from the measured texts, the logos'
        // own widths and the screen. The same rules as the UITK provider (FitButton there),
        // through the same PaymentOptionPopMetrics functions, with the spec's boxes:
        // - the POP: bottom-anchored 340..412pt; centered 375pt, growing to the widest of the
        //   header row and each row's content and badge, and to keep a badged row's content
        //   centered (CenteredBadgedButtonMinWidth), up to 500pt; both narrowed to the space
        //   between the side margins (PopWidth), which is what the overlay layout then does
        //   with the preferred width set here;
        // - each row, in this order of who gives way when it is short of room: "Continue with"
        //   beside a logo is shown whole or dropped (FitsLeadText); a lone logo narrows only if
        //   even the least badge leaves it short; the badge is whole, or ellipsized to the room
        //   the whole content leaves (BadgeRoom), never below its least (BadgeLeast, 84pt or its
        //   own width); a text-only label ellipsizes only when even that badge doesn't fit; and
        //   the content is centered, or moved left only as far as the badge needs
        //   (ButtonContentLeft).
        // The button width is worked out (PopWidth less the padding) rather than read from
        // the layout, so it can run before the layout pass and on a resize, when the layout
        // is a frame behind.
        void FitButtons()
        {
            if (m_Canvas == null || m_PopLayoutElement == null) return;
            var scale = m_Canvas.scaleFactor;
            if (scale <= 0f) return;
            var screen = ScreenPixels();
            var available = screen.x / scale - m_SafeLeft - m_SafeRight - 2f * c_PopSideMargin;
            var centered = m_CurrentLayout.Centered;

            float popWidth;
            if (!centered)
            {
                m_PopLayoutElement.preferredWidth = PaymentOptionPopMetrics.BottomAnchoredMaxWidth;
                popWidth = PaymentOptionPopMetrics.PopWidth(false, available, 0f);
            }
            else
            {
                var content = HeaderWidth();
                foreach (var fit in m_ButtonFits)
                {
                    // Against no width of its own: centered, each decision is made against
                    // the width the POP would grow to for it (ButtonWidthFor).
                    var row = FitRow(fit, 0f, true, available, apply: false);
                    content = Mathf.Max(content, Mathf.Max(row.Ask, row.CenteredMinWidth));
                }
                content += 2f * c_PopPadding;
                m_PopLayoutElement.preferredWidth = PaymentOptionPopMetrics.CenteredPreferredWidth(content);
                popWidth = PaymentOptionPopMetrics.PopWidth(true, available, content);
            }

            var buttonWidth = popWidth - 2f * c_PopPadding;
            foreach (var fit in m_ButtonFits)
                FitRow(fit, buttonWidth, centered, available, apply: true);
        }

        // The header row's width: its text, the gap and the cancel button.
        float HeaderWidth()
            => m_HeaderTextLabel != null
                ? TextWidth(m_HeaderTextLabel, m_HeaderTextLabel.text) + c_HeaderSpacing + c_CancelButtonSize
                : 0f;

        RowFit FitRow(ButtonFit fit, float buttonWidth, bool centered, float available, bool apply)
        {
            const float insets = 2f * c_ButtonSidePadding;
            // No TextLayoutSlack (UI Toolkit's allowance for laying a text out wider than it
            // measures): UGUI lays a text out at exactly the width TextWidth gives.
            const float slack = 0f;
            const float badgeGap = PaymentOptionPopMetrics.ButtonBadgeGap;
            const float badgeOffset = PaymentOptionPopMetrics.BadgeRightOffset - c_ButtonSidePadding;
            const float badgeChrome = 2f * PaymentOptionPopMetrics.BadgePadding;

            float badgeNatural = 0f, badgeLeast = 0f;
            if (fit.BadgeLabel != null)
            {
                badgeNatural = TextWidth(fit.BadgeLabel, fit.BadgeText) + badgeChrome;
                badgeLeast = PaymentOptionPopMetrics.BadgeLeast(badgeNatural,
                    TextWidth(fit.BadgeLabel, k_Ellipsis) + badgeChrome);
            }
            var reserve = PaymentOptionPopMetrics.BadgeReserve(badgeNatural, badgeGap, badgeOffset);
            var leastReserve = PaymentOptionPopMetrics.BadgeReserve(badgeLeast, badgeGap, badgeOffset);

            var labelWidth = LabelWidth(fit.Label, fit.LabelText);
            var showLabel = true;
            var logoFitted = 0f;
            float contentWidth;
            // The text on both sides of the logo is shown or dropped together.
            var hasLead = fit.LabelText.Length > 0;
            var leadPart = hasLead ? labelWidth + slack + fit.LogoGap : 0f;
            var trailWidth = fit.Trail != null ? LabelWidth(fit.Trail, fit.TrailText) : 0f;
            var trailPart = fit.Trail != null ? fit.LogoGap + trailWidth + slack : 0f;
            if (fit.Logo != null)
            {
                var required = insets + leadPart + fit.LogoWidth + trailPart + reserve;
                showLabel = PaymentOptionPopMetrics.FitsLeadText(centered, available, buttonWidth, required);
                // Alone, the logo keeps its width while the badge can give way, and is drawn
                // smaller (aspect kept) only when even the least badge leaves it short.
                logoFitted = showLabel ? fit.LogoWidth
                    : Mathf.Clamp(PaymentOptionPopMetrics.ButtonWidthFor(centered, available, buttonWidth,
                        insets + fit.LogoWidth + leastReserve) - insets - leastReserve, 0f, fit.LogoWidth);
                contentWidth = showLabel ? leadPart + logoFitted + trailPart : logoFitted;
            }
            else
            {
                // The label comes first, whole: the badge gives way to it, down to its least.
                contentWidth = labelWidth + slack;
            }

            var badgeWidth = 0f;
            var badgeWhole = true;
            var centeredMinWidth = 0f;
            if (fit.BadgeLabel != null)
            {
                var room = PaymentOptionPopMetrics.ButtonWidthFor(centered, available, buttonWidth,
                    insets + contentWidth + reserve);
                var badgeRoom = PaymentOptionPopMetrics.BadgeRoom(room, insets, contentWidth, badgeGap, badgeOffset, badgeLeast);
                badgeWhole = badgeNatural <= badgeRoom;
                badgeWidth = badgeWhole ? badgeNatural : badgeRoom;
                // Centered, a whole badge: the width that keeps the content centered, if the POP
                // can grow to it.
                if (centered && badgeWhole)
                    centeredMinWidth = PaymentOptionPopMetrics.CenteredBadgedButtonMinWidth(available, contentWidth,
                        PaymentOptionPopMetrics.BadgeReserve(badgeNatural + slack, badgeGap, badgeOffset));
            }
            var badgeReserve = PaymentOptionPopMetrics.BadgeReserve(badgeWidth, badgeGap, badgeOffset);
            var row = new RowFit(insets + contentWidth + badgeReserve, centeredMinWidth);
            if (!apply)
                return row;

            // The content: never wider than what the badge leaves (a text-only label is
            // ellipsized to it), at the place ButtonContentLeft gives it.
            var contentRoom = Mathf.Max(0f, buttonWidth - insets - badgeReserve);
            var drawnWidth = Mathf.Min(contentWidth, contentRoom);
            var label = (RectTransform)fit.Label.transform;
            if (fit.Logo != null)
            {
                var logoHeight = fit.LogoWidth > 0f ? fit.LogoHeight * logoFitted / fit.LogoWidth : fit.LogoHeight;
                fit.Label.gameObject.SetActive(showLabel && hasLead);
                SetLabelText(fit.Label, fit.LabelText);
                // Beside the Apple lockup, "Continue with" sits on the lockup's baseline. Centering
                // both boxes doesn't do that: the label's line box is the font's, taller below the
                // baseline than the lockup's lettering is, so the text sat about 3pt low.
                var labelY = fit.LogoBaseline.HasValue
                    ? fit.LogoBaseline.Value * logoHeight - LabelBaselineAboveCenter(fit.Label, labelWidth + slack, c_LabelHeight)
                    : 0f;
                SetLeft(label, 0f, labelWidth + slack, c_LabelHeight, labelY);
                var logoLeft = showLabel ? leadPart : 0f;
                SetLeft((RectTransform)fit.Logo.transform, logoLeft, logoFitted, logoHeight);
                if (fit.Trail != null)
                {
                    fit.Trail.gameObject.SetActive(showLabel);
                    var trailY = fit.LogoBaseline.HasValue
                        ? fit.LogoBaseline.Value * logoHeight - LabelBaselineAboveCenter(fit.Trail, trailWidth + slack, c_LabelHeight)
                        : 0f;
                    SetLeft((RectTransform)fit.Trail.transform, logoLeft + logoFitted + fit.LogoGap, trailWidth + slack, c_LabelHeight, trailY);
                }
            }
            else
            {
                // Text only: the TextMeshPro label is "Continue with" before a logo, never ellipsized.
                if (fit.Label is Text text)
                    text.text = contentWidth <= contentRoom ? fit.LabelText : Ellipsize(text, fit.LabelText, drawnWidth);
                SetLeft(label, 0f, drawnWidth, c_LabelHeight);
            }

            fit.Content.anchoredPosition = new Vector2(
                PaymentOptionPopMetrics.ButtonContentLeft(buttonWidth, drawnWidth, badgeReserve), 0f);
            fit.Content.sizeDelta = new Vector2(drawnWidth, c_ButtonHeight);

            if (fit.Badge != null && fit.BadgeLabel != null)
            {
                fit.Badge.sizeDelta = new Vector2(badgeWidth, PaymentOptionPopMetrics.BadgeHeight);
                fit.BadgeLabel.text = badgeWhole ? fit.BadgeText : Ellipsize(fit.BadgeLabel, fit.BadgeText, badgeWidth - badgeChrome);
            }
            return row;
        }

        // Anchored to its parent's left edge, vertically centered.
        static void SetLeft(RectTransform rt, float x, float width, float height, float y = 0f)
        {
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
        }

        static RectTransform NewLeftAnchoredChild(Transform parent, string name, params Type[] components)
        {
            var rt = (RectTransform)NewUiChild(parent, name, components).transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            return rt;
        }

        const string k_Ellipsis = "…";

        // Labels and badges are one line: a line break in a developer's badge (or a store's
        // name) would otherwise draw a second line outside the button or the pill, as
        // HorizontalWrapMode.Overflow still honors explicit breaks.
        static string OneLine(string value)
            => value.IndexOfAny(k_LineBreaks) < 0 ? value : string.Join(" ", value.Split(k_LineBreaks, StringSplitOptions.RemoveEmptyEntries));

        static readonly char[] k_LineBreaks = { '\r', '\n', '\u2028', '\u2029' };

        // A text's width in canvas units (pt), as UGUI lays it out on one line.
        static float TextWidth(Text text, string value)
        {
            var settings = text.GetGenerationSettings(Vector2.zero);
            return text.cachedTextGeneratorForLayout.GetPreferredWidth(value, settings) / text.pixelsPerUnit;
        }

        // A label's width (LabelWidth), text (SetLabelText) and baseline (LabelBaselineAboveCenter),
        // whichever component draws it (ButtonFit.Label).
        static float LabelWidth(Graphic label, string value)
        {
#if IAP_UGUI_TMP
            if (label is TMP_Text tmp)
                return tmp.GetPreferredValues(value).x;
#endif
            return label is Text text ? TextWidth(text, value) : 0f;
        }

        static void SetLabelText(Graphic label, string value)
        {
#if IAP_UGUI_TMP
            if (label is TMP_Text tmp)
            {
                tmp.text = value;
                return;
            }
#endif
            if (label is Text text)
                text.text = value;
        }

        static float LabelBaselineAboveCenter(Graphic label, float width, float height)
        {
#if IAP_UGUI_TMP
            if (label is TMP_Text tmp)
                return TmpBaselineAboveCenter(tmp);
#endif
            return label is Text text ? BaselineAboveCenter(text, width, height) : 0f;
        }

#if IAP_UGUI_TMP
        // The TextMeshPro label's baseline above its rect's vertical center, in pt, as TextMesh Pro
        // lays it out: its first line's baseline, in the rect's local space, whose origin is the
        // pivot (left-middle, NewLeftAnchoredChild). Middle alignment centers the line in the rect,
        // so its height doesn't matter. Laid out even while hidden (ignoreActiveState), as the
        // Text's is.
        static float TmpBaselineAboveCenter(TMP_Text tmp)
        {
            tmp.ForceMeshUpdate(ignoreActiveState: true);
            var info = tmp.textInfo;
            if (info == null || info.lineCount < 1)
                return 0f;
            var rt = tmp.rectTransform;
            return info.lineInfo[0].baseline + (rt.pivot.y - 0.5f) * rt.rect.height;
        }
#endif

        // How far a text's baseline is above the vertical center of a width x height rect it is
        // drawn in (negative: below), in pt, as UGUI's generator lays it out: from the bottom of
        // an "H" set with the text's own font, size, style and alignment. The baseline is the
        // font's line metrics, not the text's, so it holds for any label.
        static float BaselineAboveCenter(Text text, float width, float height)
        {
            var settings = text.GetGenerationSettings(new Vector2(width, height));
            // A pivot at the rect's center: the generator then reports positions from it.
            settings.pivot = new Vector2(0.5f, 0.5f);
            s_BaselineProbe ??= new TextGenerator();
            if (!s_BaselineProbe.Populate("H", settings))
                return 0f;
            var verts = s_BaselineProbe.verts;
            if (verts.Count < 4)
                return 0f;
            var bottom = float.MaxValue;
            for (var i = 0; i < 4; i++)
                bottom = Mathf.Min(bottom, verts[i].position.y);
            return bottom / text.pixelsPerUnit;
        }

        static TextGenerator? s_BaselineProbe;

        // UGUI's Text has no ellipsis of its own (it clips or overflows): the longest start of
        // the text that fits in maxWidth with "…" after it, or "…" alone.
        internal static string Ellipsize(Text text, string value, float maxWidth)
        {
            if (TextWidth(text, value) <= maxWidth)
                return value;
            int lo = 0, hi = value.Length - 1;
            while (lo < hi)
            {
                var mid = (lo + hi + 1) / 2;
                if (TextWidth(text, Cut(value, mid)) <= maxWidth)
                    lo = mid;
                else
                    hi = mid - 1;
            }
            return Cut(value, lo);
        }

        // The first length chars and "…", never splitting a surrogate pair (an emoji in a
        // developer's badge), which would leave half a character before the ellipsis.
        internal static string Cut(string value, int length)
        {
            if (length > 0 && char.IsHighSurrogate(value[length - 1]))
                length--;
            return value.Substring(0, length).TrimEnd() + k_Ellipsis;
        }

        // brand is null exactly when button has no brand (DescribeButton, then WithoutBrand
        // when the logo failed to load). Colors by kind (PaymentOptionPopPalette): the text
        // always contrasts its own button, whatever the theme. Content on one line: "Continue
        // with" and the logo, or the whole label (the webshop's has no icon), then the badge;
        // FitButtons places them. The webshop choice goes through the VM too, which fires
        // RedirectToWebshop on the PSP extension.
        Button BuildOptionButton(PurchaseOptionViewModel.PurchaseOpt opt,
            PurchaseOptionViewModel.ButtonDescriptor button, Sprite? brand)
        {
            var dark = m_CurrentTheme.Dark;

            var go = new GameObject(opt.IsWebshop ? "WebshopButton" : $"Pay_{opt.StoreName}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(m_ButtonBox, worldPositionStays: false);

            var img = go.GetComponent<Image>();
            img.color = PaymentOptionPopPalette.ButtonBackground(button.Kind, dark);
            img.sprite = ButtonSprite;
            img.type = Image.Type.Sliced;

            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = c_ButtonHeight;

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => { if (m_Disposed) return; SetVisible(false); _ = m_VM.CompleteChoice(opt); });

            var content = NewLeftAnchoredChild(go.transform, "Content");
            var apple = button.Kind == PurchaseOptionViewModel.ButtonKind.Apple;
            var textColor = PaymentOptionPopPalette.ButtonText(button.Kind, dark);
            // The text on either side of a logo, or the whole label: a legacy Text, or beside the
            // Apple lockup a TextMeshProUGUI in the system font's Medium face (Apple's guidelines
            // for the mark), where the project has TextMesh Pro set up (AppleSystemTmpFont) and the
            // face has every character of the text (AppleSystemFontCanDraw; the legacy Text's
            // font falls back to other OS fonts).
            Graphic NewButtonText(string name, string text)
            {
                Graphic t;
#if IAP_UGUI_TMP
                var mediumFont = apple && brand != null ? AppleSystemTmpFont(Application.platform) : null;
                if (mediumFont != null && PaymentOptionPopMetrics.AppleSystemFontCanDraw(text))
                    t = AddTmpText(content, name, text, mediumFont, PaymentOptionPopMetrics.AppleButtonFontSize, textColor);
                else
#endif
                    t = AddButtonText(content, name, text, apple, textColor);
                var tRt = (RectTransform)t.transform;
                tRt.anchorMin = tRt.anchorMax = new Vector2(0f, 0.5f);
                tRt.pivot = new Vector2(0f, 0.5f);
                // Taps land on the button's own image.
                t.raycastTarget = false;
                return t;
            }

            var labelText = brand != null ? button.LeadText ?? "" : button.Label;
            var label = NewButtonText("Label", labelText);
            var fit = new ButtonFit(rt, content, label, labelText);
            if (brand != null)
            {
                var logoRt = NewLeftAnchoredChild(content, "Logo", typeof(Image));
                fit.Logo = logoRt.GetComponent<Image>();
                fit.Logo.sprite = brand;
                fit.Logo.preserveAspect = true;
                fit.Logo.raycastTarget = false;
                fit.LogoHeight = PaymentOptionPopMetrics.ButtonLogoHeight(button.Kind);
                // The sprite's own rect: a sprite cut from a larger texture (an atlas) is not
                // the texture's shape.
                fit.LogoWidth = fit.LogoHeight * brand.rect.width / brand.rect.height;
                fit.LogoGap = PaymentOptionPopMetrics.ButtonLogoGap(button.Kind);
                if (button.Kind == PurchaseOptionViewModel.ButtonKind.Apple)
                    fit.LogoBaseline = 0.5f - PaymentOptionPopMetrics.AppleLockupBaseline;
                if (button.TrailText is { Length: > 0 } trail)
                {
                    fit.Trail = NewButtonText("Trail", trail);
                    fit.TrailText = OneLine(trail);
                }
            }

            if (button.Badge is { Length: > 0 } badge)
                BuildBadge(go.transform, badge, fit);

            m_ButtonFits.Add(fit);
            return btn;
        }

        // A button's label as a legacy Text: 15pt bold, or the Apple button's 17pt regular in the
        // OS system font where there is one.
        static Text AddButtonText(RectTransform content, string name, string value, bool apple, Color color)
        {
            var label = AddText(content, name, value,
                fontSize: apple ? PaymentOptionPopMetrics.AppleButtonFontSize : c_ButtonFontSize,
                bold: !apple, color: color, align: TextAnchor.MiddleLeft);
            // The Apple button's text is 17pt regular in the built-in font here; BuildOptionButton
            // draws "Continue with" in SF Medium through TextMesh Pro where it can
            // (AppleSystemTmpFont).
            // One line, never wrapped or clipped by the rect: FitButtons sizes the rect to the
            // text and ellipsizes the text itself.
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            // Plain text: a store's name is not markup, and Ellipsize cuts characters, which
            // would split a tag (see BuildBadge).
            label.supportRichText = false;
            return label;
        }

        // The spec's badge ("Bonus Reward Chip / Discount Chip"): a 20pt pill, 12pt bold text
        // with 6pt each side, vertically centered, 20pt from the button's right edge, in the
        // theme's badge colors. FitButtons sets its width.
        void BuildBadge(Transform buttonRoot, string badgeText, ButtonFit fit)
        {
            var theme = m_CurrentTheme;
            var badgeGo = NewUiChild(buttonRoot, "Badge", typeof(Image));
            var rt = (RectTransform)badgeGo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-PaymentOptionPopMetrics.BadgeRightOffset, 0f);
            rt.sizeDelta = new Vector2(PaymentOptionPopMetrics.BadgeMinWidth, PaymentOptionPopMetrics.BadgeHeight);

            var img = badgeGo.GetComponent<Image>();
            img.color = theme.Colors.BadgeBackground;
            img.sprite = BadgeSprite;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;

            var label = AddText(badgeGo.transform, "BadgeLabel", badgeText,
                fontSize: PaymentOptionPopMetrics.BadgeFontSize, bold: true, color: theme.Colors.BadgeText,
                align: TextAnchor.MiddleCenter);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            // A developer's badge is drawn as typed: with rich text, Ellipsize (which cuts
            // characters) could leave half a tag, which then draws as text, and the badge's
            // colors are the spec's anyway.
            label.supportRichText = false;
            label.raycastTarget = false;
            var labelRt = (RectTransform)label.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(PaymentOptionPopMetrics.BadgePadding, 0f);
            labelRt.offsetMax = new Vector2(-PaymentOptionPopMetrics.BadgePadding, 0f);

            fit.Badge = rt;
            fit.BadgeLabel = label;
            fit.BadgeText = OneLine(badgeText);
            label.text = fit.BadgeText;
        }

#if IAP_UGUI_TMP
        // The system font's Medium face as a TextMesh Pro font asset, for "Continue with" before
        // the Apple lockup (Apple's guidelines for the mark: SF Pro Medium), from the font file
        // and face index PaymentOptionPopMetrics finds (as the UITK provider's asset is). Null off
        // Apple platforms, where there is no Medium face, or where the project lacks TextMesh
        // Pro's resources (its "TMP Essential Resources": the TMP Settings asset and its shaders),
        // which TextMesh Pro needs to create a font asset and draw. Without them it throws, and
        // in the Editor TMP_Settings.instance opens its importer window, so they are checked
        // first without going through it. The legacy Text, in the built-in font, is drawn then.
        // Only TextMesh Pro: a legacy Font opens a variable font's default instance, Regular, and
        // no API opens a face index into one. Loaded once per domain and kept; a failure is not
        // retried.
        static TMP_FontAsset? s_AppleSystemTmpFont;
        static bool s_AppleSystemTmpFontUnavailable;

        // Takes the platform so the non-Apple path can be tested on a Mac.
        internal static TMP_FontAsset? AppleSystemTmpFont(RuntimePlatform platform)
        {
            if (!PaymentOptionPopMetrics.HasAppleSystemFont(platform) || s_AppleSystemTmpFontUnavailable)
                return null;
            if (s_AppleSystemTmpFont != null)
                return s_AppleSystemTmpFont;

            try
            {
                if (!HasTmpResources())
                {
                    Debug.unityLogger.LogIAPVerbose("AppleSystemTmpFont: no TextMesh Pro resources in the project; the Apple button keeps the legacy font");
                }
                else if (PaymentOptionPopMetrics.TryFindAppleSystemFontMedium(out var path, out var faceIndex))
                {
                    var asset = PaymentOptionPopMetrics.DropFontFaceWarning.Around(() => TMP_FontAsset.CreateFontAsset(path, faceIndex,
                        PaymentOptionPopMetrics.AppleSystemFontSamplingSize, PaymentOptionPopMetrics.AppleSystemFontPadding,
                        TextCore.LowLevel.GlyphRenderMode.SDFAA, PaymentOptionPopMetrics.AppleSystemFontAtlasSize,
                        PaymentOptionPopMetrics.AppleSystemFontAtlasSize));
                    if (asset != null && asset.faceInfo.styleName == PaymentOptionPopMetrics.AppleSystemFontWeight)
                    {
                        asset.name = $"{PaymentOptionPopMetrics.AppleSystemFontFamily} {PaymentOptionPopMetrics.AppleSystemFontWeight} (Purchase Options UI)";
                        asset.hideFlags = HideFlags.HideAndDontSave;
                        s_AppleSystemTmpFont = asset;
                        return asset;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogIAPVerbose($"AppleSystemTmpFont: failed to load: {e.Message}");
            }

            s_AppleSystemTmpFontUnavailable = true;
            return null;
        }

        // TextMesh Pro's settings asset and the shader its runtime font assets are created with
        // (the "TMP Essential Resources" import puts both in the project, the shader through the
        // materials in its Resources folder, so a player has it too).
        internal static bool HasTmpResources()
            => Resources.Load<TMP_Settings>("TMP Settings") != null
            && Shader.Find("TextMeshPro/Mobile/Distance Field") != null;

        // "Continue with" as a TextMeshProUGUI, set like the legacy Text it replaces: one line,
        // middle-left, plain text, no margins.
        static TextMeshProUGUI AddTmpText(Transform parent, string name, string text,
            TMP_FontAsset font, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, worldPositionStays: false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Normal;
            tmp.enableAutoSizing = false;
            tmp.richText = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.margin = Vector4.zero;
            tmp.color = color;
            tmp.text = text;
            return tmp;
        }
#endif

        void ClearDynamicButtons()
        {
            // Destroy is deferred to the end of the frame, so these are still children
            // of ButtonBox when SettlePopLayout force-rebuilds right after. Layout
            // groups skip inactive children, so deactivating first is what keeps the
            // POP from being measured against the old buttons plus the new ones.
            foreach (var b in m_DynamicButtons)
            {
                if (b == null) continue;
                b.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(b.gameObject);
            }
            m_DynamicButtons.Clear();
            m_ButtonFits.Clear();
        }

        static GameObject NewUiChild(Transform parent, string name, params Type[] components)
        {
            var fullList = new Type[components.Length + 1];
            fullList[0] = typeof(RectTransform);
            components.CopyTo(fullList, 1);
            var go = new GameObject(name, fullList);
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, worldPositionStays: false);
            return go;
        }

        static void StretchFull(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Text AddText(Transform parent, string name, string text,
            int fontSize, bool bold, Color color, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, worldPositionStays: false);
            var txt = go.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = text;
            txt.fontSize = fontSize;
            txt.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            txt.color = color;
            txt.alignment = align;
            return txt;
        }

        // Generates a rounded-corner sprite at runtime (in-memory Texture2D +
        // Sprite.Create). No file I/O, no AssetDatabase — Editor-only APIs are
        // unavailable here.
        //
        // Rasterized at 4x the requested pt radius, with pixelsPerUnit scaled to
        // match, so the sliced corner measures cornerPt on screen but still has
        // enough texels to stay smooth at a 3-4x device scaleFactor.
        static Sprite CreateRoundedSprite(float cornerPt)
        {
            const float oversample = 4f;
            var radius = Mathf.Max(1, Mathf.RoundToInt(cornerPt * oversample));
            // +8 keeps a few non-corner texels in the middle for the sliced center.
            var size = radius * 2 + 8;
            return CreateRoundedSprite(size, radius, 100f * oversample);
        }

        static Sprite CreateRoundedSprite(int size, int radius, float pixelsPerUnit)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var alpha = SoftRoundedAlpha(x, y, size, size, radius);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var rect = new Rect(0f, 0f, size, size);
            var pivot = new Vector2(0.5f, 0.5f);
            var border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, rect, pivot, pixelsPerUnit, extrude: 0,
                meshType: SpriteMeshType.FullRect, border: border);
        }

        static float SoftRoundedAlpha(int x, int y, int w, int h, int radius)
        {
            int cx, cy;
            var inCorner = true;
            if (x < radius && y < radius) { cx = radius; cy = radius; }
            else if (x >= w - radius && y < radius) { cx = w - radius - 1; cy = radius; }
            else if (x < radius && y >= h - radius) { cx = radius; cy = h - radius - 1; }
            else if (x >= w - radius && y >= h - radius) { cx = w - radius - 1; cy = h - radius - 1; }
            else { inCorner = false; cx = 0; cy = 0; }
            if (!inCorner) return 1f;
            var dx = x - cx;
            var dy = y - cy;
            var dist = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(radius - dist + 0.5f);
        }
    }
}
