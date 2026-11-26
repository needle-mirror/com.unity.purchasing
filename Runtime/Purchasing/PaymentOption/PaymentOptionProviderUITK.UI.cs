#if IAP_UIELEMENTS
#nullable enable
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEngine.Purchasing
{
    /// UITK hierarchy construction, partialed out so the View surface
    /// (Init / Show / Dispose) stays readable in PaymentOptionProviderUITK.cs.
    /// Mirrors the split on the UGUI side.
    internal sealed partial class PaymentOptionProviderUITK
    {
        const string k_UssResourcesPath = "com.unity.purchasing/PaymentOptionProviderModal";
        const string k_PanelSettingsResourcesPath = "com.unity.purchasing/PaymentOptionProviderPanelSettings";

        // BEM USS classes — single source of truth, mirror PaymentOptionProviderModal.uss.
        const string k_Block          = "unity-iap-picker";
        const string k_BlockDark      = "unity-iap-picker--dark";
        const string k_Overlay        = "unity-iap-picker__overlay";
        const string k_OverlayHidden  = "unity-iap-picker__overlay--hidden";
        const string k_OverlayBottom  = "unity-iap-picker__overlay--bottom";
        const string k_Sheet          = "unity-iap-picker__sheet";
        const string k_SheetFloor     = "unity-iap-picker__sheet-floor";
        const string k_Header         = "unity-iap-picker__header";
        const string k_Title          = "unity-iap-picker__title";
        const string k_Close          = "unity-iap-picker__close";
        const string k_CloseGlyph     = "unity-iap-picker__close-glyph";
        const string k_Buttons        = "unity-iap-picker__buttons";
        const string k_Button         = "unity-iap-picker__button";
        const string k_ButtonStacked  = "unity-iap-picker__button--stacked";
        // One modifier per ButtonKind, which picks the button's --color-* pair in the USS.
        // The Apple and Google Play ones keep their earlier (shipped) names.
        const string k_ButtonGeneric  = "unity-iap-picker__button--generic";
        const string k_ButtonWebshop  = "unity-iap-picker__button--webshop";
        const string k_ButtonApple    = "unity-iap-picker__button--apple-pay";
        const string k_ButtonNative   = "unity-iap-picker__button--native";
        const string k_ButtonGooglePlay = "unity-iap-picker__button--google-pay";
        const string k_ButtonCoda     = "unity-iap-picker__button--coda";
        const string k_ButtonStripe   = "unity-iap-picker__button--stripe";
        const string k_ButtonMockProvider = "unity-iap-picker__button--mock-provider";
        const string k_ButtonContent  = "unity-iap-picker__button-content";
        const string k_ButtonLabel    = "unity-iap-picker__button-label";
        const string k_ButtonLabelApple = "unity-iap-picker__button-label--apple";
        const string k_Brand          = "unity-iap-picker__brand";
        const string k_BadgeSlot      = "unity-iap-picker__badge-slot";
        const string k_Badge          = "unity-iap-picker__badge";

        // Consumer-visible, do not rename: documented as the way to reach the POP's
        // UIDocument (to add an override stylesheet or raise the panel's sortingOrder),
        // so projects look it up by this name.
        internal const string k_PanelGameObjectName = "PaymentOptionProviderUITK";

        IReadOnlyList<PurchaseOptionViewModel.PurchaseOpt>? m_CurrentOpts;

        // The POP's own panel: a DontDestroyOnLoad GameObject with a UIDocument on its
        // own PanelSettings instance. Owning the panel is what makes one UI unit one pt
        // (PanelSettings.scale, below) regardless of how the game scales its own panels,
        // and it keeps the POP out of the game's visual tree, so a game root that
        // collapses to zero size or clips can no longer push the modal off-screen.
        //
        // The cost is that the game's theme and stylesheets no longer cascade into the
        // POP. Overrides of the --color-* variables in PaymentOptionProviderModal.uss go
        // on the POP's own root instead. The panel exists after a call to Apply*Theme,
        // Apply*Layout or ValidatePurchaseOption (not SetLocale or ShowPurchaseOption), so:
        //   provider.ApplyLightTheme();
        //   GameObject.Find("PaymentOptionProviderUITK").GetComponent<UIDocument>()
        //       .rootVisualElement.styleSheets.Add(myOverrides);
        //   await provider.ShowPurchaseOption(...);
        // The package sheet is attached to that same root first, so an override sheet
        // added after it wins at equal specificity. With several providers alive, Find
        // returns any one of them.
        VisualElement BuildPanel(StyleSheet styleSheet, PanelSettings template)
        {
            // A copy of the shipped template, so per-provider changes (scale, target
            // texture, a raised sortingOrder) never touch the asset. The template carries:
            // - ConstantPixelSize, the UITK equivalent of the UGUI provider's
            //   ConstantPixelSize CanvasScaler; the scale itself is set below.
            //   ScaleWithScreenSize would pin the POP to a fraction of the screen, so it
            //   would be physically tiny on a phone and huge on a tablet.
            // - sortingOrder 10000, the same headroom above typical game UI as the UGUI
            //   provider's Canvas. Panels and Screen Space - Overlay canvases are sorted
            //   against each other by this value, and so are their EventSystem raycasters.
            //   Projects that layer UI higher raise it after first use via the
            //   "PaymentOptionProviderUITK" GameObject's UIDocument.panelSettings.
            // - an empty theme (PaymentOptionProviderTheme.tss). The POP styles itself from
            //   its own USS and sets its font below; the theme exists because a theme-less
            //   PanelSettings warns that "UI will not render properly", and on 2022.3 it
            //   does so in OnEnable, before code could assign one — hence an asset rather
            //   than ScriptableObject.CreateInstance.
            m_PanelSettings = Object.Instantiate(template);
            m_PanelSettings.name = k_PanelGameObjectName;
            m_PanelSettings.targetTexture = TargetTexture;

            // Inactive while wiring so the UIDocument attaches once, to the right panel,
            // rather than first to none.
            m_Root = new GameObject(k_PanelGameObjectName);
            m_Root.SetActive(false);
            Object.DontDestroyOnLoad(m_Root);
            m_Document = m_Root.AddComponent<UIDocument>();
            m_Document.panelSettings = m_PanelSettings;
            m_Root.SetActive(true);

            var root = m_Document.rootVisualElement;
            // A UIDocument without a visualTreeAsset gets a root that can collapse to zero
            // size; pinning it to the panel's edges gives the overlay the full screen.
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.right = 0;
            root.style.top = 0;
            root.style.bottom = 0;
            // The root covers the screen for the whole lifetime of the provider, including
            // while the POP is hidden; it must never be what a tap on the game's UI hits.
            root.pickingMode = PickingMode.Ignore;
            // The empty theme supplies no default font, and without one every label renders
            // as nothing. Inherited, so the root covers the whole modal. Same built-in font
            // as the UGUI provider.
            root.style.unityFontDefinition = new StyleFontDefinition(
                FontDefinition.FromFont(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")));
            root.styleSheets.Add(styleSheet);

            // Resizes and rotations reach the panel as a new root size. There is no
            // willRenderCanvases equivalent to poll, and the root's geometry is exactly
            // the signal: it follows the screen (or target texture) size, in panel units.
            root.RegisterCallback<GeometryChangedEvent>(OnPanelGeometryChanged);
            // The safe area usually changes with the screen size (a rotation), which the
            // geometry change above covers, but not always: turning a phone from one landscape
            // side to the other moves the notch without resizing anything. So it is also
            // checked on every panel update while the POP is shown; reading it is cheap. So are the
            // buttons' colors, which a consumer sheet can change without any event (UpdateLogoTone).
            root.schedule.Execute(OnPanelUpdate).Every(0);
            UpdatePanelScale();
            return root;
        }

        void OnPanelGeometryChanged(GeometryChangedEvent evt)
        {
            UpdatePanelScale();
            // The centered POP's room depends on the screen as well as on its own width.
            FitButtons();
        }

        void OnPanelUpdate()
        {
            CheckSafeArea();
            UpdateLogoTones();
        }

        void CheckSafeArea()
        {
            if (m_PanelSettings == null || m_Overlay == null || m_Overlay.ClassListContains(k_OverlayHidden)) return;
            ScreenPixels(m_PanelSettings, out var width, out var height);
            if (SafeAreaPixels(m_PanelSettings, width, height) == m_AppliedSafeArea) return;
            UpdatePanelScale();
            FitButtons();
        }

        // The screen's (or target texture's) size in pixels.
        static void ScreenPixels(PanelSettings panelSettings, out int width, out int height)
        {
            var target = panelSettings.targetTexture;
            width = target != null ? target.width : Screen.width;
            height = target != null ? target.height : Screen.height;
        }

        // Screen.safeArea, or the test's stand-in, kept within the screen. A panel drawn to a
        // texture isn't on the screen, so the screen's safe area doesn't apply to it.
        Rect SafeAreaPixels(PanelSettings panelSettings, int width, int height)
        {
            var area = SafeAreaOverride
                ?? (panelSettings.targetTexture != null ? new Rect(0f, 0f, width, height) : Screen.safeArea);
            return PaymentOptionPopMetrics.ClampSafeArea(area, width, height);
        }

        Rect m_AppliedSafeArea;
        // The safe area's insets in pt, as last applied to the overlay's padding.
        float m_SafeLeft, m_SafeRight, m_SafeTop, m_SafeBottom;

        // Same arithmetic as the UGUI provider (PaymentOptionPopMetrics), applied as the
        // panel scale, against the safe area rather than the whole screen: the POP and its
        // margins have to fit in what the notch and the home indicator leave. Vertically it
        // reserves the bottom-anchored 48pt margin, or centered 16pt above and below
        // (ReservedHeight), as on UGUI. Converges: a new scale changes
        // the root's size in panel units and so raises GeometryChangedEvent again, but the
        // factor depends only on the pixel sizes, dpi and content, so the second pass resolves
        // the same value and stops.
        void UpdatePanelScale()
        {
            if (m_PanelSettings == null) return;
            ScreenPixels(m_PanelSettings, out var width, out var height);
            // First: the layout mode decides the reserved height the scale is resolved for.
            // The automatic layout follows the device and the screen's size.
            m_Centered = CenteredOverride ?? PaymentOptionPopMetrics.AutomaticCentered(Platform, DeviceModel, DpiOverride ?? Screen.dpi, width, height);
            m_Overlay?.EnableInClassList(k_OverlayBottom, !m_Centered);
            var safe = SafeAreaPixels(m_PanelSettings, width, height);
            var scale = PaymentOptionPopMetrics.ResolveScaleFactor(DpiOverride ?? Screen.dpi,
                Platform,
                Mathf.FloorToInt(safe.width), Mathf.FloorToInt(safe.height),
                m_CurrentOpts?.Count ?? 0,
                PaymentOptionPopMetrics.ReservedHeight(m_Centered));
            if (!Mathf.Approximately(m_PanelSettings.scale, scale))
                m_PanelSettings.scale = scale;
            m_AppliedSafeArea = safe;
            PaymentOptionPopMetrics.SafeAreaInsets(safe, width, height, scale,
                out m_SafeLeft, out m_SafeRight, out m_SafeTop, out m_SafeBottom);
            ApplyOverlayPadding();
        }

        // The margins, measured from the safe area's edges: the overlay's padding is the
        // spec's margin plus the inset on each side, so the POP is laid out (centered, or
        // bottom-anchored and horizontally centered) within the safe area. Set inline, over
        // the USS values, which are the same margins without insets.
        void ApplyOverlayPadding()
        {
            if (m_Overlay == null) return;
            var centered = m_Centered;
            SetPadding(m_Overlay.style, PaymentOptionPopMetrics.SideMargin + m_SafeLeft,
                PaymentOptionPopMetrics.SideMargin + m_SafeRight,
                (centered ? PaymentOptionPopMetrics.CenteredVerticalMargin : 0f) + m_SafeTop,
                (centered ? PaymentOptionPopMetrics.CenteredVerticalMargin : PaymentOptionPopMetrics.BottomAnchoredMargin) + m_SafeBottom);
        }

        static void SetPadding(IStyle style, float left, float right, float top, float bottom)
        {
            if (!IsInline(style.paddingLeft, left)) style.paddingLeft = left;
            if (!IsInline(style.paddingRight, right)) style.paddingRight = right;
            if (!IsInline(style.paddingTop, top)) style.paddingTop = top;
            if (!IsInline(style.paddingBottom, bottom)) style.paddingBottom = bottom;
        }

        static bool IsInline(StyleLength length, float value)
            => length.keyword == StyleKeyword.Undefined && length.value.value == value;

        // The width between the side margins, within the safe area, in pt.
        float AvailableWidth(UIDocument document)
            => document.rootVisualElement.layout.width - 2f * PaymentOptionPopMetrics.SideMargin - m_SafeLeft - m_SafeRight;

        void BuildHierarchy(VisualElement root)
        {
            m_Modal = new VisualElement { name = "iapPickerModal", pickingMode = PickingMode.Ignore };
            m_Modal.AddToClassList(k_Block);
            // Theme and layout chosen before a rebuild survive it.
            if (m_DarkMode) m_Modal.AddToClassList(k_BlockDark);
            root.Add(m_Modal);

            m_Overlay = new VisualElement { name = "purchaseOverlay" };
            m_Overlay.AddToClassList(k_Overlay);
            // Bottom-anchored by default, matching the UGUI provider.
            m_Overlay.AddToClassList(k_OverlayBottom);
            m_Overlay.AddToClassList(k_OverlayHidden);
            // Tap-outside-to-dismiss. The event bubbles up from whatever was clicked,
            // so the target check is what distinguishes the dimmed backdrop from the
            // POP and its buttons — without it, every click would dismiss.
            var overlay = m_Overlay;
            overlay.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target as VisualElement == overlay)
                    Cancel();
            });
            // The panel going away while a choice is pending (the GameObject destroyed
            // without Dispose) resolves the choice as cancelled, so the caller's await
            // returns and the next Show isn't refused as "already in progress". Our own
            // DestroyPanel clears m_Overlay first, so its deferred detach is ignored and
            // can't cancel a choice pending on a newer panel.
            overlay.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                if (overlay == m_Overlay)
                    m_VM.CancelPendingChoice();
            });
            m_Modal.Add(m_Overlay);

            m_Sheet = new VisualElement { name = "purchaseSheet" };
            m_Sheet.AddToClassList(k_Sheet);
            m_Overlay.Add(m_Sheet);

            var header = new VisualElement { name = "header" };
            header.AddToClassList(k_Header);
            m_Sheet.Add(header);

            m_Title = new Label(PaymentOptionProviderTranslations.Translate(PaymentOptionProviderTranslations.Keys.Header, CurrentLocale));
            m_Title.AddToClassList(k_Title);
            header.Add(m_Title);

            // 44pt hit target laid out as the 32pt visible box (negative margins in USS),
            // so it overflows the header row instead of growing it — as on UGUI.
            m_CloseX = new Button { name = "closeX", text = "" };
            m_CloseX.AddToClassList(k_Close);
            m_CloseX.clicked += Cancel;
            header.Add(m_CloseX);

            // Drawn rather than typed: the built-in font's "×" is a small, heavy glyph,
            // while the spec's cancel icon is a thin cross about 16pt across.
            m_CloseGlyph = new VisualElement { name = "closeGlyph", pickingMode = PickingMode.Ignore };
            m_CloseGlyph.AddToClassList(k_CloseGlyph);
            m_CloseGlyph.generateVisualContent += DrawCloseGlyph;
            m_CloseX.Add(m_CloseGlyph);
            // The cross is drawn in the button's (inherited) text color, read when its mesh is
            // generated, so anything that can change that color has to repaint it: hover, a
            // theme change (Apply*Theme), and the --color-* variables being re-resolved — which
            // is how a consumer stylesheet added to the root (or the dark class) reaches it.
            // The block declares those variables, so it gets CustomStyleResolvedEvent whenever
            // they resolve. Show repaints it as well, as a backstop.
            m_CloseX.RegisterCallback<PointerEnterEvent>(_ => m_CloseGlyph?.MarkDirtyRepaint());
            m_CloseX.RegisterCallback<PointerLeaveEvent>(_ => m_CloseGlyph?.MarkDirtyRepaint());
            m_Modal.RegisterCallback<CustomStyleResolvedEvent>(_ =>
            {
                m_CloseGlyph?.MarkDirtyRepaint();
                // A consumer stylesheet arriving: refit the buttons too (their badges and
                // padding report through geometry changes as well; this is the backstop).
                FitButtons();
            });

            m_ButtonBox = new VisualElement { name = "buttons", pickingMode = PickingMode.Ignore };
            m_ButtonBox.AddToClassList(k_Buttons);
            m_Sheet.Add(m_ButtonBox);

            // Zero-height, CenteredDefaultWidth wide: keeps the centered POP's content-sized
            // width from going below 375pt while still letting the overlay shrink it on a
            // small screen. A min-width can't do both, because min-width beats max-width.
            // Centered, FitButton widens it for a badged row whose content stays centered
            // (UpdateSheetFloor).
            m_SheetFloor = new VisualElement { name = "sheetFloor", pickingMode = PickingMode.Ignore };
            m_SheetFloor.AddToClassList(k_SheetFloor);
            m_Sheet.Add(m_SheetFloor);
        }

        // Stroke width of the cancel cross, in pt. Not in the spec; measured off the mocks (~2pt).
        internal const float k_CloseGlyphStroke = 2f;

        // For tests: how many times UpdateLogoTone has looked a logo variant up.
        internal static int LogoLookupsForTests;

        // For tests: the color and stroke width the cancel cross was last drawn with.
        internal Color? CloseGlyphDrawnColor { get; private set; }
        internal float? CloseGlyphDrawnStroke { get; private set; }

        void DrawCloseGlyph(MeshGenerationContext context)
        {
            var element = context.visualElement;
            var rect = element.contentRect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            CloseGlyphDrawnColor = element.resolvedStyle.color;
            CloseGlyphDrawnStroke = k_CloseGlyphStroke;
            // Inset by half the stroke so the round caps stay inside the glyph box.
            var inset = k_CloseGlyphStroke * 0.5f;
            var painter = context.painter2D;
            painter.strokeColor = element.resolvedStyle.color;
            painter.lineWidth = k_CloseGlyphStroke;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin + inset, rect.yMin + inset));
            painter.LineTo(new Vector2(rect.xMax - inset, rect.yMax - inset));
            painter.MoveTo(new Vector2(rect.xMax - inset, rect.yMin + inset));
            painter.LineTo(new Vector2(rect.xMin + inset, rect.yMax - inset));
            painter.Stroke();
        }

        string CurrentLocale => m_VM.Locale;

        void ApplyStrings()
        {
            if (m_Title != null)
                m_Title.text = PaymentOptionProviderTranslations.Translate(PaymentOptionProviderTranslations.Keys.Header, CurrentLocale);
        }

        void BuildButtons(IReadOnlyList<PurchaseOptionViewModel.PurchaseOpt> opts)
        {
            m_CurrentOpts = opts;
            ApplyStrings();
            ClearDynamicButtons();
            if (m_ButtonBox == null) return;

            for (var i = 0; i < opts.Count; i++)
            {
                var opt = opts[i];
                var button = PurchaseOptionViewModel.DescribeButton(opt, CurrentLocale);
                // The theme's variant to start with; once the button's colors have settled,
                // UpdateLogoTone swaps it if a consumer sheet has changed them.
                var tone = PaymentOptionPopPalette.LogoTone(button.Kind, m_DarkMode);
                var brand = button.HasBrand ? PaymentBrandRegistry.GetBrand(opt.StoreName, tone) : null;
                if (button.HasBrand && brand == null)
                    button = button.WithoutBrand();

                var btn = BuildOptionButton(i, opt, button, brand, tone);
                // The 12pt gap goes above every button but the first; USS has no sibling selectors.
                if (i > 0)
                    btn.AddToClassList(k_ButtonStacked);
                m_ButtonBox.Add(btn);
                m_DynamicButtons.Add(btn);
            }

            // Button count feeds PaymentOptionPopMetrics.RequiredPopHeight, so the scale
            // has to follow the content, not just the screen size.
            UpdatePanelScale();
        }

        // Its colors are the USS's (one modifier class per kind, so consumers can override
        // them), which mirror PaymentOptionPopPalette.ButtonBackground / ButtonText; the
        // text color is set on the button and inherited, so it always contrasts the
        // button's own background, whatever the theme. Content is centered on one line:
        // "Continue with" and the logo, or the whole label, then the badge.
        //
        // brand is null exactly when button has no brand (DescribeButton, then
        // WithoutBrand when the logo failed to load). The webshop choice goes through the
        // VM too, which fires RedirectToWebshop on the PSP extension.
        //
        // With a badge the row is [content][badge slot]. The slot is as wide as the badge
        // and its gaps (the badge's reserve) and takes whatever room is left, so the badge
        // sits at its right end, BadgeRightOffset from the button's edge, and the content
        // can never run under it: with too little room, the content shrinks (the label
        // ellipsizes). C# (FitButton) caps the slot when the badge has to give way
        // (PaymentOptionPopMetrics.BadgeRoom), and moves the content right by translation, per
        // PaymentOptionPopMetrics.ButtonContentLeft: centered in the button when there is
        // room, otherwise only as far as the badge allows. A translation, not a spacer: it
        // doesn't count towards the width the row asks for, so it can't feed back into the
        // centered POP's width (which made layout oscillate) or round the label a pixel
        // short. USS alone can't express the rule: zero-basis growing spacers split the
        // free space differently on 2022.3 once one of them has a min-width. The centered
        // POP's growth for a centered badged row is the sheet floor's width instead,
        // derived from the content's and the badge's own widths only.
        Button BuildOptionButton(int index, PurchaseOptionViewModel.PurchaseOpt opt,
            PurchaseOptionViewModel.ButtonDescriptor button, Sprite? brand, PaymentBrandRegistry.Tone tone)
        {
            var btn = new Button { name = opt.IsWebshop ? "webshopButton" : $"pay_{index}", text = "" };
            btn.AddToClassList(k_Button);
            btn.AddToClassList(KindClass(button.Kind));

            var content = new VisualElement { pickingMode = PickingMode.Ignore };
            content.AddToClassList(k_ButtonContent);
            btn.Add(content);

            Label NewButtonLabel(string? text)
            {
                var l = new Label(text);
                l.AddToClassList(k_ButtonLabel);
                if (button.Kind == PurchaseOptionViewModel.ButtonKind.Apple)
                {
                    // 17pt from the USS; the OS system font's Medium face, where there is one, from
                    // here (AppleSystemFont). FitButton sets it on the lockup's baseline
                    // (AlignLabelBaseline).
                    l.AddToClassList(k_ButtonLabelApple);
                    // Only for text the face can draw (AppleSystemFontCanDraw): a font asset built from
                    // the SF file has no Hangul, say, and would draw it as empty boxes.
                    var systemFont = AppleSystemFont(Application.platform);
                    if (systemFont != null && (text == null || PaymentOptionPopMetrics.AppleSystemFontCanDraw(text)))
                    {
                        l.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(systemFont));
#if UNITY_6000_3_OR_NEWER
                        // The advanced text generator draws nothing with a font asset built from a
                        // font file (LoadSystemFontMedium), and measures it 0pt wide; the standard one
                        // draws it, as on 6000.0, which has no other.
                        l.style.unityTextGenerator = TextGeneratorType.Standard;
#endif
                    }
                }
                return l;
            }

            var label = NewButtonLabel(brand != null ? button.LeadText : button.Label);
            content.Add(label);
            var fit = new ButtonFit(btn, content, label);
            if (brand != null)
            {
                fit.Logo = BuildBrand(brand, button.Kind);
                fit.LogoStore = opt.StoreName;
                fit.LogoKind = button.Kind;
                fit.LogoTone = tone;
                fit.LogoGap = PaymentOptionPopMetrics.ButtonLogoGap(button.Kind);
                fit.LogoWidth = fit.Logo.style.width.value.value;
                content.Add(fit.Logo);
                if (!string.IsNullOrEmpty(button.TrailText))
                {
                    fit.Trail = NewButtonLabel(button.TrailText);
                    fit.Trail.style.marginLeft = fit.LogoGap;
                    content.Add(fit.Trail);
                }
            }

            if (!string.IsNullOrEmpty(button.Badge))
            {
                var slot = new VisualElement { pickingMode = PickingMode.Ignore };
                slot.AddToClassList(k_BadgeSlot);
                fit.Slot = slot;
                // The content at the left inset and the slot at the right one even when the slot
                // is capped and leaves free space (FitButton positions the content itself).
                btn.style.justifyContent = Justify.SpaceBetween;
                fit.Badge = new Label(button.Badge) { pickingMode = PickingMode.Ignore };
                fit.Badge.AddToClassList(k_Badge);
                slot.Add(fit.Badge);
                btn.Add(slot);
                // Its width or margins can change without the button's or the content's (a
                // consumer stylesheet added later): the reserve beside it has to follow.
                fit.Badge.RegisterCallback<GeometryChangedEvent>(_ => FitButton(fit));
            }

            if (fit.Badge != null || fit.Logo != null)
            {
                // The button for its width; the content for its own, which changes without the
                // button's when "Continue with" is shown or hidden on a bottom-anchored POP.
                btn.RegisterCallback<GeometryChangedEvent>(_ => FitButton(fit));
                content.RegisterCallback<GeometryChangedEvent>(_ => FitButton(fit));
                m_ButtonFits.Add(fit);
            }

            btn.clicked += () =>
            {
                // Defensive: in tests, a disposed UITK panel delivers no clicks even without this guard.
                if (m_Disposed) return;
                m_Overlay?.AddToClassList(k_OverlayHidden);
                _ = m_VM.CompleteChoice(opt);
            };
            return btn;
        }

        // The Apple In-App Purchase button's lead text is "SF Pro 17pt/20, medium" in the spec, and
        // Apple's guidelines for the In-App Purchase mark ask for SF Pro Medium at the mark's size:
        // the OS system font, which only Apple platforms (and the macOS Editor) have.
        //
        // The system font is one variable font file (SFNS.ttf on macOS, SFUI.ttf on iOS), whose
        // family is "System Font" and whose weights are named instances. Asked for by family and
        // style (FontAsset.CreateFontAsset(family, style)), TextCore only finds the file's default
        // instance, Regular: "System Font" "Medium" returns null on macOS. FreeType opens a named
        // instance as face index (instance << 16), though, and TextCore passes the face index
        // through, so the Medium instance is found in the file
        // (PaymentOptionPopMetrics.TryFindAppleSystemFontMedium) and the asset built from it. Font.CreateDynamicFontFromOSFont with FontDefinition.FromFont is
        // no alternative: UI Toolkit cannot build a font asset from an OS font on 2022.3 ("Unable
        // to load font face"), nor from SF on 6000.
        //
        // Where no Medium instance is found, or off Apple platforms, the label keeps the panel's
        // built-in font, at 17pt regular. ponytail: iOS is untested on device; the iOS simulator
        // runtime's SFUI.ttf has the same "System Font" family and a Medium instance (fifth) as
        // macOS's SFNS.ttf.
        //
        // Loaded once per domain and kept (HideAndDontSave): one small dynamic atlas shared by
        // every provider. A destroyed asset (Unity-null) is loaded again; a failure is not retried.
        static TextCore.Text.FontAsset? s_AppleSystemFont;
        static bool s_AppleSystemFontUnavailable;

        // Takes the platform so the non-Apple path can be tested on a Mac.
        internal static TextCore.Text.FontAsset? AppleSystemFont(RuntimePlatform platform)
        {
            if (!PaymentOptionPopMetrics.HasAppleSystemFont(platform) || s_AppleSystemFontUnavailable)
                return null;
            if (s_AppleSystemFont != null)
                return s_AppleSystemFont;

            var medium = LoadSystemFontMedium();
            if (medium != null)
            {
                medium.name = $"{PaymentOptionPopMetrics.AppleSystemFontFamily} {PaymentOptionPopMetrics.AppleSystemFontWeight} (Purchase Options UI)";
                medium.hideFlags = HideFlags.HideAndDontSave;
                s_AppleSystemFont = medium;
                return medium;
            }

            Debug.unityLogger.LogIAPVerbose("AppleSystemFont: no Medium face of the system font; the Apple button keeps the built-in font");
            s_AppleSystemFontUnavailable = true;
            return null;
        }

        // The system font's Medium face, from its font file (PaymentOptionPopMetrics finds it).
        // Null where there is none, or it fails. Internal for tests.
        internal static TextCore.Text.FontAsset? LoadSystemFontMedium()
        {
            try
            {
                if (!PaymentOptionPopMetrics.TryFindAppleSystemFontMedium(out var path, out var faceIndex))
                    return null;
                var asset = PaymentOptionPopMetrics.DropFontFaceWarning.Around(() => TextCore.Text.FontAsset.CreateFontAsset(path, faceIndex,
                    PaymentOptionPopMetrics.AppleSystemFontSamplingSize, PaymentOptionPopMetrics.AppleSystemFontPadding,
                    TextCore.LowLevel.GlyphRenderMode.SDFAA, PaymentOptionPopMetrics.AppleSystemFontAtlasSize, PaymentOptionPopMetrics.AppleSystemFontAtlasSize));
                if (asset != null && asset.faceInfo.styleName == PaymentOptionPopMetrics.AppleSystemFontWeight)
                    return asset;
            }
            catch (System.Exception e)
            {
                Debug.unityLogger.LogIAPVerbose($"AppleSystemFont: {PaymentOptionPopMetrics.AppleSystemFontFamily} {PaymentOptionPopMetrics.AppleSystemFontWeight} failed to load: {e.Message}");
            }
            return null;
        }

        // The parts of a button whose layout depends on measured widths, which USS can't
        // express: where the content starts beside a badge, and whether "Continue with"
        // fits whole before a logo.
        sealed class ButtonFit
        {
            public readonly Button Button;
            public readonly VisualElement Content;
            // The whole label, or the text before the logo (possibly empty).
            public readonly Label Label;
            public Label? Trail;
            public Image? Logo;
            // What the logo was loaded for, so UpdateLogoTone can load its other variant.
            public string LogoStore = "";
            public PurchaseOptionViewModel.ButtonKind LogoKind;
            public PaymentBrandRegistry.Tone LogoTone;
            // The button's colors as UpdateLogoTone last read them: it acts once they have settled.
            public Color? LastBackground;
            public Color? LastText;
            public float LogoGap;
            // The logo's own width (BuildBrand): FitButton narrows it when even the logo alone
            // has no room, and has to know what to give back.
            public float LogoWidth;
            public Label? Badge;
            public VisualElement? Slot;
            // What FitButton last set (null: nothing), so a pass that changes nothing writes
            // nothing: the slot's max-width, the badge's basis, and the width the row asks of
            // the centered POP (UpdateSheetFloor).
            public float? SlotCap;
            public float? BadgeBasis;
            public float? MinWidth;
            // How far AlignLabelBaseline last moved the labels down (null: not moved).
            public float? LabelShift;

            public ButtonFit(Button button, VisualElement content, Label label)
            {
                Button = button;
                Content = content;
                Label = label;
            }
        }

        readonly List<ButtonFit> m_ButtonFits = new();
        VisualElement? m_SheetFloor;
        // The floor's width as last set inline (null: the USS's).
        float? m_SheetFloorWidth;

        // The centered POP's floor is the widest of its badged rows' centered widths
        // (ButtonFit.MinWidth), when any is wider than the USS's 335pt: a fixed-width child is
        // what the sheet's content-sized width follows on 2022.3 as well. (A min-width on the
        // button would have been simpler, but 2022.3 then sizes the sheet at its max-width.)
        void UpdateSheetFloor()
        {
            if (m_SheetFloor == null) return;
            float? widest = null;
            foreach (var fit in m_ButtonFits)
                if (fit.MinWidth.HasValue && (!widest.HasValue || fit.MinWidth.Value > widest.Value))
                    widest = fit.MinWidth;
            if (widest.HasValue && widest.Value <= PaymentOptionPopMetrics.CenteredDefaultWidth - 2f * PaymentOptionPopMetrics.Padding)
                widest = null;
            if (SameValue(m_SheetFloorWidth, widest)) return;
            m_SheetFloorWidth = widest;
            m_SheetFloor.style.width = widest.HasValue ? new StyleLength(widest.Value) : new StyleLength(StyleKeyword.Null);
        }

        void FitButtons()
        {
            foreach (var fit in m_ButtonFits)
                FitButton(fit);
        }

        static float TextWidth(TextElement element)
            => element.MeasureTextSize(element.text, 0f, VisualElement.MeasureMode.Undefined,
                0f, VisualElement.MeasureMode.Undefined).x;

        // The badge's own width, as it asks for it rather than as laid out (which is what
        // FitButton decides): its text and padding, within a consumer's min- and max-width.
        static float BadgeNaturalWidth(Label badge)
            => Mathf.Max(BadgeTextWidth(badge), BadgeLeast(badge));

        static float BadgeTextWidth(Label badge)
        {
            var style = badge.resolvedStyle;
            var width = TextWidth(badge) + BadgeChrome(style);
            var cap = style.maxWidth;
            if (cap.keyword == StyleKeyword.Undefined && cap.value > 0f)
                width = Mathf.Min(width, cap.value);
            return width;
        }

        // The narrowest the badge is drawn (PaymentOptionPopMetrics.BadgeLeast): 84pt, or its
        // own width if narrower, or a consumer's min-width if wider.
        static float BadgeLeast(Label badge)
        {
            var style = badge.resolvedStyle;
            var ellipsis = badge.MeasureTextSize(k_Ellipsis, 0f, VisualElement.MeasureMode.Undefined,
                0f, VisualElement.MeasureMode.Undefined).x + BadgeChrome(style);
            var min = style.minWidth;
            return PaymentOptionPopMetrics.BadgeLeast(BadgeTextWidth(badge), ellipsis,
                min.keyword == StyleKeyword.Undefined ? min.value : 0f);
        }

        static float BadgeChrome(IResolvedStyle style)
            => style.paddingLeft + style.paddingRight + style.borderLeftWidth + style.borderRightWidth;

        const string k_Ellipsis = "\u2026";

        // Run on every geometry change of the button, its content, its badge and the panel, when
        // the block's custom styles are resolved (a consumer stylesheet), and on a layout mode
        // change. Decides, in this order of who gives way when the row is short of room:
        // "Continue with" beside a logo (shown whole or dropped, FitsLeadText); a lone logo's
        // width (narrowed only if even the least badge leaves it no room); the badge's width
        // (whole, or ellipsized to BadgeRoom beside the whole content, never below its least: a
        // text-only label is shortened for it only when even that doesn't fit); centered, the button width that keeps the content centered
        // (CenteredBadgedButtonMinWidth, applied as the sheet floor's width); and the content's
        // position (ButtonContentLeft).
        //
        // No feedback loop: what the row asks for (the lead text, the logo's width, the badge
        // cap, the centered width) is derived from measured texts, the logo's own width, the laid-out
        // padding and margins and the screen, and from the button's current width only as a
        // lower bound on the room (ButtonWidthFor): a capped badge leaves the row asking for no
        // more than that room, and a capped row never asks for the centered width. So the centered
        // POP resizes at most once in response, and the next pass settles on the same values,
        // within the frame: UI Toolkit re-runs layout after GeometryChangedEvent handlers change
        // styles.
        void FitButton(ButtonFit fit)
        {
            if (fit.Button.panel == null || m_Document == null) return;
            var buttonWidth = fit.Button.layout.width;
            if (float.IsNaN(buttonWidth) || buttonWidth <= 0f) return;

            var centered = m_Centered;
            var available = AvailableWidth(m_Document);
            // The button's own padding and border as laid out, not the spec's: the content box
            // is what the content and the badge share.
            var buttonStyle = fit.Button.resolvedStyle;
            var insetLeft = buttonStyle.paddingLeft + buttonStyle.borderLeftWidth;
            var insetRight = buttonStyle.paddingRight + buttonStyle.borderRightWidth;
            var insets = insetLeft + insetRight;
            const float slack = PaymentOptionPopMetrics.TextLayoutSlack;

            float badgeWidth = 0f, badgeLeast = 0f, badgeGap = 0f, badgeOffset = 0f;
            if (fit.Badge != null)
            {
                badgeWidth = BadgeNaturalWidth(fit.Badge);
                badgeLeast = BadgeLeast(fit.Badge);
                badgeGap = fit.Badge.resolvedStyle.marginLeft;
                badgeOffset = fit.Badge.resolvedStyle.marginRight;
            }

            // The badge's reserve at its own width, and at its least.
            var reserve = PaymentOptionPopMetrics.BadgeReserve(badgeWidth, badgeGap, badgeOffset);
            var leastReserve = PaymentOptionPopMetrics.BadgeReserve(badgeLeast, badgeGap, badgeOffset);

            var labelWidth = TextWidth(fit.Label);
            float contentWidth;
            if (fit.Logo != null)
            {
                var logoWidth = fit.LogoWidth;
                // The text on both sides of the logo is shown or dropped together.
                var hasLead = !string.IsNullOrEmpty(fit.Label.text);
                var leadPart = hasLead ? labelWidth + fit.LogoGap : 0f;
                // With the slack it is laid out with below, so a trail that fits is never ellipsized.
                var trailPart = fit.Trail != null ? fit.LogoGap + TextWidth(fit.Trail) + slack : 0f;
                var required = insets + leadPart + logoWidth + trailPart + reserve;
                var show = PaymentOptionPopMetrics.FitsLeadText(centered, available, buttonWidth, required);
                var display = show && hasLead ? DisplayStyle.Flex : DisplayStyle.None;
                if (fit.Label.resolvedStyle.display != display)
                    fit.Label.style.display = display;
                if (fit.Trail != null)
                {
                    var trailDisplay = show ? DisplayStyle.Flex : DisplayStyle.None;
                    if (fit.Trail.resolvedStyle.display != trailDisplay)
                        fit.Trail.style.display = trailDisplay;
                }
                // The gap is the space after "Continue with"; alone, the logo has none.
                var gap = show && hasLead ? fit.LogoGap : 0f;
                if (!Mathf.Approximately(fit.Logo.resolvedStyle.marginLeft, gap))
                    fit.Logo.style.marginLeft = gap;
                // Alone, the logo keeps its width while the badge can give way (down to its
                // least, BadgeLeast), and is drawn smaller (ScaleToFit keeps its aspect) only when even
                // that leaves it short. The spec's boxes always leave it room; a consumer
                // stylesheet widening the badge's least width or the padding may not, and the
                // logo doesn't shrink by itself (flex-shrink: 0), so without this it would run
                // under the badge. Centered, against the width the POP would grow to for the
                // logo, as for the lead text: narrowing it against the current width would stop
                // the POP growing for it.
                var logoRequired = insets + logoWidth + leastReserve;
                var logoFitted = show ? logoWidth
                    : Mathf.Clamp(PaymentOptionPopMetrics.ButtonWidthFor(centered, available, buttonWidth, logoRequired)
                        - insets - leastReserve, 0f, logoWidth);
                if (!Mathf.Approximately(fit.Logo.resolvedStyle.width, logoFitted))
                    fit.Logo.style.width = logoFitted;
                AlignLabelBaseline(fit, show);
                // All of it is kept beside the badge (with a pt for "Continue with" being laid
                // out a little wider than it measures, so it is never ellipsized).
                contentWidth = show
                    ? (hasLead ? labelWidth + slack + fit.LogoGap : 0f) + logoFitted
                        + trailPart
                    : logoFitted;
            }
            else
            {
                // The label comes first, whole (as on UGUI): the badge gives way to it, down to its
                // least (84pt, or its own width if narrower). Only when even that doesn't fit
                // (BadgeRoom's floor) does the label shrink, and then it ellipsizes rather than
                // run under the badge.
                contentWidth = labelWidth + slack;
            }

            if (fit.Badge == null || fit.Slot == null)
                return;

            // The badge: whole if the room the row gets leaves it its width, else ellipsized to
            // that room (the slot's max-width; the badge shrinks inside it). A px max-width on
            // the slot, not the badge: a consumer's own max-width on the badge stays in force.
            var room = PaymentOptionPopMetrics.ButtonWidthFor(centered, available, buttonWidth,
                insets + contentWidth + reserve);
            var badgeRoom = PaymentOptionPopMetrics.BadgeRoom(room, insets, contentWidth, badgeGap, badgeOffset, badgeLeast);
            var whole = badgeWidth <= badgeRoom;
            var slotCap = whole ? (float?)null : badgeRoom + badgeGap + badgeOffset;
            if (!SameValue(fit.SlotCap, slotCap))
            {
                fit.SlotCap = slotCap;
                fit.Slot.style.maxWidth = slotCap.HasValue ? new StyleLength(slotCap.Value) : new StyleLength(StyleKeyword.Null);
            }

            // And the badge's basis is the width decided here, not its text's: an ellipsized
            // text (by the room, or by a consumer's max-width) can measure narrower than the box
            // it is given (6000.7 measures it at its shortened text's width), which would leave
            // the badge that much narrower, below its least, and the row asking for less than
            // the reserve it was fitted with. Not a min-width: that is the consumer's, and
            // BadgeLeast reads it.
            var basis = whole ? badgeWidth : badgeRoom;
            if (!SameValue(fit.BadgeBasis, basis))
            {
                fit.BadgeBasis = basis;
                fit.Badge.style.flexBasis = basis;
            }

            // Centered, a whole badge: the row asks for the width that keeps its content
            // centered (through the sheet's floor), if the POP can grow to it (the badge's reserve with the same pt of slack
            // as the content, as both may be laid out that much wider than they measure).
            var minWidth = centered && whole
                ? PaymentOptionPopMetrics.CenteredBadgedButtonMinWidth(available, contentWidth,
                    PaymentOptionPopMetrics.BadgeReserve(badgeWidth + slack, badgeGap, badgeOffset), insetLeft, insetRight)
                : 0f;
            var buttonMin = minWidth > 0f ? (float?)minWidth : null;
            if (!SameValue(fit.MinWidth, buttonMin))
            {
                fit.MinWidth = buttonMin;
                UpdateSheetFloor();
            }

            // Laid out at the left inset, at its own width or squeezed beside the badge (and then
            // ButtonContentLeft leaves it there); moved to where the rule puts it, against the
            // badge as laid out (2022.3 draws text a pt wider than it measures). A display change
            // above relayouts the content, which comes back here.
            var laidOutWidth = fit.Content.layout.width;
            var laidOutBadge = fit.Badge.layout.width;
            if (float.IsNaN(laidOutWidth) || float.IsNaN(laidOutBadge))
                return;
            var laidOutReserve = PaymentOptionPopMetrics.BadgeReserve(laidOutBadge, badgeGap, badgeOffset);
            var offset = PaymentOptionPopMetrics.ButtonContentLeft(buttonWidth, laidOutWidth, laidOutReserve, insetLeft, insetRight)
                         - fit.Content.layout.x;
            if (!Mathf.Approximately(fit.Content.resolvedStyle.translate.x, offset))
                fit.Content.style.translate = new Translate(offset, 0f);
        }

        // Beside the Apple lockup, the text before it ("Continue with") and after it (a translation
        // whose brand comes first) stand on the lockup's baseline (Apple's guideline for the mark),
        // as on UGUI. The content row centers every box (align-items: center), which doesn't do
        // that: a label's line box is the font's, deeper below the baseline than the lockup's
        // lettering is, so the text sat about 1.5pt low. Both labels share the font, size and row,
        // so one shift, measured on whichever is shown, moves both. Moved by translation, not a
        // margin: the row's height and the width it asks for don't change. Run by FitButton, on
        // every geometry change of the button and its content.
        static void AlignLabelBaseline(ButtonFit fit, bool show)
        {
            float? shift = null;
            if (show && fit.Logo != null && fit.LogoKind == PurchaseOptionViewModel.ButtonKind.Apple)
            {
                var measured = MeasuredLabel(fit);
                var label = measured.layout;
                var logo = fit.Logo.layout;
                var baseline = BaselineFromTop(measured, label.height);
                if (baseline.HasValue && !float.IsNaN(label.y) && !float.IsNaN(logo.y) && !float.IsNaN(logo.height))
                    shift = logo.y + PaymentOptionPopMetrics.AppleLockupBaseline * logo.height - (label.y + baseline.Value);
            }

            if (SameValue(fit.LabelShift, shift)) return;
            fit.LabelShift = shift;
            var translate = shift.HasValue
                ? new StyleTranslate(new Translate(0f, shift.Value))
                : new StyleTranslate(StyleKeyword.Null);
            fit.Label.style.translate = translate;
            if (fit.Trail != null)
                fit.Trail.style.translate = translate;
        }

        // For tests: whether every Apple row showing "Continue with" before its lockup stands on
        // the lockup's baseline, that is AlignLabelBaseline has computed its shift and the label has
        // resolved the translate it set. The shift is applied after layout, so a capture taken
        // before this can catch the label still centered in its box. Rows that drop "Continue with"
        // (hidden label) have no shift and are skipped, as are rows whose font's metrics can't be
        // read (the built-in font off Apple editors on 6000+), which AlignLabelBaseline leaves
        // centered.
        internal bool LabelsOnBaselineForTests
        {
            get
            {
                foreach (var fit in m_ButtonFits)
                {
                    if (fit.Logo == null || fit.LogoKind != PurchaseOptionViewModel.ButtonKind.Apple
                        || fit.Label.resolvedStyle.display == DisplayStyle.None)
                        continue;
                    if (!HasReadableFontMetrics(MeasuredLabel(fit)))
                        continue;
                    if (!fit.LabelShift.HasValue
                        || Mathf.Abs(fit.Label.resolvedStyle.translate.y - fit.LabelShift.Value) > 0.01f)
                        return false;
                }
                return true;
            }
        }

        // The label AlignLabelBaseline measures: "Continue with", or the text after the lockup when
        // there is none before it.
        static TextElement MeasuredLabel(ButtonFit fit)
            => !string.IsNullOrEmpty(fit.Label.text) || fit.Trail == null ? fit.Label : fit.Trail;

        // Whether BaselineFromTop can read the label's font. Off Apple editors on 6000+, the Apple
        // label keeps the panel's built-in font, whose metrics TextCore can't read; AlignLabelBaseline
        // then leaves it centered. Internal for tests. pointSize is an int on 2022.3, a float on 6000+.
        internal static bool HasReadableFontMetrics(TextElement label)
            => FaceInfoOf(label.resolvedStyle.unityFontDefinition) is { pointSize: > 0 };

        // How far a one-line label's baseline is below the top of its box, height pt tall, in pt;
        // null when its font's metrics can't be read. TextCore sets the line, its font's ascent
        // over its descent, centered in the box (middle alignment), whatever the text.
        internal static float? BaselineFromTop(TextElement label, float height)
        {
            if (float.IsNaN(height))
                return null;
            var style = label.resolvedStyle;
            var face = FaceInfoOf(style.unityFontDefinition);
            if (!face.HasValue || face.Value.pointSize <= 0f)
                return null;
            var f = face.Value;
            var scale = style.fontSize / f.pointSize * (f.scale > 0f ? f.scale : 1f);
            var ascent = f.ascentLine * scale;
            var descent = f.descentLine * scale;
            return (height - (ascent - descent)) / 2f + ascent;
        }

        // The font's metrics: a font asset's own, or a Font's as the font engine reads them
        // (once per Font; the panel's built-in font is the only one this sees).
        static TextCore.FaceInfo? FaceInfoOf(FontDefinition definition)
        {
            if (definition.fontAsset != null)
                return definition.fontAsset.faceInfo;
            var font = definition.font;
            if (font == null)
                return null;
            if (s_FontFaces.TryGetValue(font, out var cached))
                return cached;
            if (TextCore.LowLevel.FontEngine.LoadFontFace(font, PaymentOptionPopMetrics.AppleSystemFontSamplingSize) != TextCore.LowLevel.FontEngineError.Success)
                return null;
            var face = TextCore.LowLevel.FontEngine.GetFaceInfo();
            s_FontFaces[font] = face;
            return face;
        }

        static readonly Dictionary<Font, TextCore.FaceInfo> s_FontFaces = new();

        // The logo variant follows the button's resolved colors, not the theme: a consumer sheet
        // that overrides a button's --color-* pair (say the native store white with black text on
        // the light theme) would otherwise leave the light logo on a light button, invisible.
        // PaymentOptionPopPalette.LogoTone(background, text) picks the variant that reads like the
        // text; on the package's own colors it is the theme's (LogoTone(kind, dark)), so nothing
        // changes without an override. A brand that ships one variant only (Stripe's white
        // wordmark) keeps it, and the missing variant is looked up once per change of tone. Both
        // variants of every bundled logo have the same size; the width is recomputed anyway, and
        // FitButton works from it.
        //
        // Run on every panel update while the POP is shown. No event proved a reliable signal for a
        // consumer sheet added or removed while it shows: read in the block's
        // CustomStyleResolvedEvent, the buttons' colors were still the old ones, and a button with a
        // custom property of its own was not always sent one when only the variables it inherits
        // changed. The colors are only acted on once they have settled (the same on two updates in
        // a row): the buttons' colors are transitioned (0.12s), from transparent and the default
        // text color when they first show, and read mid-way they would flip the theme's variant for
        // a few frames. So a new button keeps the theme's variant until its colors settle, and an
        // overridden one swaps then, at the end of its fade-in. A translucent background is
        // composited over the POP's own background, which is what shows through it.
        void UpdateLogoTones()
        {
            if (m_Overlay == null || m_Sheet == null || m_Overlay.ClassListContains(k_OverlayHidden)) return;
            var behind = m_Sheet.resolvedStyle.backgroundColor;
            foreach (var fit in m_ButtonFits)
                UpdateLogoTone(fit, behind);
        }

        static void UpdateLogoTone(ButtonFit fit, Color behind)
        {
            if (fit.Logo == null) return;
            var style = fit.Button.resolvedStyle;
            var background = style.backgroundColor;
            var text = style.color;
            var settled = fit.LastBackground == background && fit.LastText == text;
            fit.LastBackground = background;
            fit.LastText = text;
            if (!settled) return;
            var tone = PaymentOptionPopPalette.LogoTone(Color.Lerp(behind, background, background.a), text);
            if (tone == fit.LogoTone) return;
            // Recorded even when the brand has no such variant (Stripe's white wordmark only): the
            // logo stays as it is, and the lookup isn't repeated on every update. A later change
            // back to the other tone loads that variant as usual.
            fit.LogoTone = tone;
            LogoLookupsForTests++;
            var sprite = PaymentBrandRegistry.GetBrand(fit.LogoStore, tone);
            if (sprite == null) return;
            fit.Logo.sprite = sprite;
            var width = LogoWidth(sprite, fit.LogoKind);
            if (Mathf.Approximately(width, fit.LogoWidth)) return;
            fit.LogoWidth = width;
            fit.Logo.style.width = width;
        }

        static bool SameValue(float? a, float? b)
            => a is float x ? b is float y && Mathf.Approximately(x, y) : b is null;

        static string KindClass(PurchaseOptionViewModel.ButtonKind kind) => kind switch
        {
            PurchaseOptionViewModel.ButtonKind.Webshop      => k_ButtonWebshop,
            PurchaseOptionViewModel.ButtonKind.Apple        => k_ButtonApple,
            PurchaseOptionViewModel.ButtonKind.NativeText   => k_ButtonNative,
            PurchaseOptionViewModel.ButtonKind.GooglePlay   => k_ButtonGooglePlay,
            PurchaseOptionViewModel.ButtonKind.Coda         => k_ButtonCoda,
            PurchaseOptionViewModel.ButtonKind.Stripe       => k_ButtonStripe,
            PurchaseOptionViewModel.ButtonKind.MockProvider => k_ButtonMockProvider,
            // Generic, and Branded: a logo on the generic button.
            _                                               => k_ButtonGeneric,
        };

        // Sized and spaced in C#, not USS: the width follows the sprite's aspect ratio, and
        // the height and gap differ per brand.
        internal static Image BuildBrand(Sprite sprite, PurchaseOptionViewModel.ButtonKind kind)
        {
            var img = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit };
            img.AddToClassList(k_Brand);
            var height = PaymentOptionPopMetrics.ButtonLogoHeight(kind);
            img.style.height = height;
            img.style.marginLeft = PaymentOptionPopMetrics.ButtonLogoGap(kind);
            img.style.width = LogoWidth(sprite, kind);
            return img;
        }

        // The logo's own width: its height for the brand, at the sprite's aspect ratio. The
        // sprite's own rect, not its texture's: a sprite cut from a larger texture (an atlas) is
        // not the texture's shape, and the Image draws only the rect.
        static float LogoWidth(Sprite sprite, PurchaseOptionViewModel.ButtonKind kind)
            => PaymentOptionPopMetrics.ButtonLogoHeight(kind) * sprite.rect.width / sprite.rect.height;

        void ClearDynamicButtons()
        {
            foreach (var b in m_DynamicButtons)
                b?.RemoveFromHierarchy();
            m_DynamicButtons.Clear();
            m_ButtonFits.Clear();
            UpdateSheetFloor();
        }
    }
}

#endif
