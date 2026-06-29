#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace UnityEngine.Purchasing
{
    /// UGUI hierarchy + procedural sprite generation, partialed out so the
    /// behavioural surface (Init / Show / Resolve / Theme / Dispose) stays
    /// readable in PaymentOptionProvider.cs. Runtime-only — no AssetDatabase
    /// or EncodeToPNG.
    internal sealed partial class PaymentOptionProvider
    {
        // Non-themed palette (same in light and dark).
        static readonly Color32 c_OverlayBg        = new Color32(0,   0,   0,   166);

        readonly struct Theme
        {
            public readonly Color SheetBg;
            public readonly Color TitleText;
            public readonly Color CloseGlyph;
            public readonly Color NativeBg;
            public readonly Color BrandedBg;
            public readonly Color GenericBg;
            public readonly Color ButtonText;
            public readonly Color BadgeBg;
            public readonly Color BadgeText;
            public readonly PaymentBrandRegistry.Tone BrandTone;

            public Theme(Color sheetBg, Color titleText, Color closeGlyph,
                Color nativeBg, Color brandedBg, Color genericBg,
                Color buttonText, Color badgeBg, Color badgeText,
                PaymentBrandRegistry.Tone brandTone)
            {
                SheetBg = sheetBg; TitleText = titleText; CloseGlyph = closeGlyph;
                NativeBg = nativeBg; BrandedBg = brandedBg; GenericBg = genericBg;
                ButtonText = buttonText; BadgeBg = badgeBg; BadgeText = badgeText;
                BrandTone = brandTone;
            }
        }

        static readonly Theme s_LightTheme = new Theme(
            sheetBg:    new Color32(255, 255, 255, 255),
            titleText:  new Color32(26,  26,  26,  255),
            closeGlyph: new Color32(140, 140, 144, 255),
            nativeBg:   new Color32(255, 255, 255, 255),
            brandedBg:  new Color32(245, 242, 232, 255),
            genericBg:  new Color32(232, 232, 232, 255),
            buttonText: new Color32(26,  26,  26,  255),
            badgeBg:    new Color32(200, 239, 215, 255),
            badgeText:  new Color32(15,  58,  31,  255),
            brandTone:  PaymentBrandRegistry.Tone.DarkOnLight);

        static readonly Theme s_DarkTheme = new Theme(
            sheetBg:    new Color32(28,  28,  32,  255),
            titleText:  new Color32(245, 245, 245, 255),
            closeGlyph: new Color32(180, 180, 190, 255),
            nativeBg:   new Color32(45,  45,  50,  255),
            brandedBg:  new Color32(50,  48,  42,  255),
            genericBg:  new Color32(60,  60,  66,  255),
            buttonText: new Color32(245, 245, 245, 255),
            badgeBg:    new Color32(28,  110, 64,  255),
            badgeText:  new Color32(245, 250, 246, 255),
            brandTone:  PaymentBrandRegistry.Tone.LightOnDark);

        Theme m_CurrentTheme = s_LightTheme;
        IReadOnlyList<PurchaseOptionViewModel.PurchaseOpt>? m_CurrentOpts;

        // === Procedural sprite caches (cheap; outlive provider instances) ===
        static Sprite? s_SheetSprite;
        static Sprite? s_ButtonSprite;
        static Sprite? s_BadgeSprite;
        static Sprite SheetSprite  => s_SheetSprite  ??= CreateRoundedSprite(72, 32, sliced: true);
        static Sprite ButtonSprite => s_ButtonSprite ??= CreateRoundedSprite(48, 18, sliced: true);
        static Sprite BadgeSprite  => s_BadgeSprite  ??= CreateRoundedSprite(40, 20, sliced: true);

        void BuildHierarchy()
        {
            m_Root = new GameObject(nameof(PaymentOptionProvider),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Object.DontDestroyOnLoad(m_Root);

            var canvas = m_Root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Sit above typical game/HUD canvases (0–1000) with plenty of headroom.
            // Projects whose UI layers higher can override by grabbing the
            // "PaymentOptionProvider" GameObject's Canvas after first Show.
            canvas.sortingOrder = 10000;

            var scaler = m_Root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;

            var overlayGo = NewUiChild(m_Root.transform, "PurchaseOverlay",
                typeof(Image), typeof(CanvasGroup));
            StretchFull(overlayGo);
            var overlayImg = overlayGo.GetComponent<Image>();
            overlayImg.color = c_OverlayBg;
            m_Overlay = overlayGo.GetComponent<CanvasGroup>();
            m_Overlay.alpha = 0f;
            m_Overlay.interactable = false;
            m_Overlay.blocksRaycasts = false;

            var sheetGo = NewUiChild(overlayGo.transform, "PurchaseSheet",
                typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var sheetRt = (RectTransform)sheetGo.transform;
            sheetRt.anchorMin = sheetRt.anchorMax = new Vector2(0.5f, 0.5f);
            sheetRt.pivot = new Vector2(0.5f, 0.5f);
            sheetRt.anchoredPosition = Vector2.zero;
            sheetRt.sizeDelta = new Vector2(880f, 0f);
            m_Sheet = sheetGo.transform;
            m_SheetImage = sheetGo.GetComponent<Image>();
            m_SheetImage.color = m_CurrentTheme.SheetBg;
            m_SheetImage.sprite = SheetSprite;
            m_SheetImage.type = Image.Type.Sliced;

            var sheetLayout = sheetGo.GetComponent<VerticalLayoutGroup>();
            sheetLayout.padding = new RectOffset(36, 36, 28, 28);
            sheetLayout.spacing = 14f;
            sheetLayout.childAlignment = TextAnchor.UpperCenter;
            sheetLayout.childControlWidth = true;
            sheetLayout.childControlHeight = true;
            sheetLayout.childForceExpandWidth = true;
            sheetLayout.childForceExpandHeight = false;
            var sheetFitter = sheetGo.GetComponent<ContentSizeFitter>();
            sheetFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sheetFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildHeaderRow(sheetGo.transform);
            BuildHeaderSpacer(sheetGo.transform);
        }

        // Transparent spacer between the header and the first option button.
        // VerticalLayoutGroup spacing (14px) alone isn't enough breathing room
        // and bumping it would loosen button-to-button gaps too.
        static void BuildHeaderSpacer(Transform sheet)
        {
            var go = new GameObject("HeaderSpacer", typeof(RectTransform), typeof(LayoutElement));
            ((RectTransform)go.transform).SetParent(sheet, worldPositionStays: false);
            go.GetComponent<LayoutElement>().preferredHeight = 30f;
        }

        void BuildHeaderRow(Transform sheet)
        {
            var rowGo = NewUiChild(sheet, "Header",
                typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 4, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var le = rowGo.GetComponent<LayoutElement>();
            le.preferredHeight = 56f;

            m_TitleLabel = AddText(rowGo.transform, "Title", "How would you like to pay?",
                fontSize: 30, bold: true, color: m_CurrentTheme.TitleText, align: TextAnchor.MiddleLeft);
            var titleLe = m_TitleLabel.gameObject.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1f;
            titleLe.preferredHeight = 40f;

            BuildCloseX(rowGo.transform);
        }

        void BuildCloseX(Transform parent)
        {
            var go = new GameObject("CloseX",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, worldPositionStays: false);
            rt.sizeDelta = new Vector2(40f, 40f);

            m_CloseBgImage = go.GetComponent<Image>();
            m_CloseBgImage.color = new Color(0f, 0f, 0f, 0f); // invisible bg — X only

            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = 40f;
            le.preferredHeight = 40f;

            m_CloseX = go.GetComponent<Button>();

            m_CloseGlyphLabel = AddText(go.transform, "Label", "×",
                fontSize: 40, bold: false, color: m_CurrentTheme.CloseGlyph,
                align: TextAnchor.MiddleCenter);
            var labelRt = (RectTransform)m_CloseGlyphLabel.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = new Vector2(0f, 4f);
        }

        void BuildButtons(IReadOnlyList<PurchaseOptionViewModel.PurchaseOpt> opts)
        {
            m_CurrentOpts = opts;
            ClearDynamicButtons();
            if (m_Sheet == null) return;

            foreach (var opt in opts)
            {
                if (opt.IsWebshop)
                {
                    m_DynamicButtons.Add(BuildWebshopButton(opt));
                    continue;
                }

                var isApplePay = PaymentBrandRegistry.IsApplePay(opt.StoreName);
                var isGooglePay = PaymentBrandRegistry.IsGooglePay(opt.StoreName);
                var isCoda = PaymentBrandRegistry.IsCoda(opt.StoreName);
                var isStripe = PaymentBrandRegistry.IsStripe(opt.StoreName);
                // Apple Pay, Google Pay and CODA: button color contrasts the
                // sheet, so the brand sprite must invert too. Stripe: purple
                // wordmark lockup, force the white logo on both sheet tones.
                var tone = isApplePay || isGooglePay || isCoda ? Invert(m_CurrentTheme.BrandTone)
                    : isStripe ? PaymentBrandRegistry.Tone.LightOnDark
                    : m_CurrentTheme.BrandTone;
                var brand = PaymentBrandRegistry.GetBrand(opt.StoreName, tone);
                var btn = BuildOptionButton(opt, brand, isApplePay, isGooglePay, isCoda, isStripe);
                m_DynamicButtons.Add(btn);
            }
        }

        static PaymentBrandRegistry.Tone Invert(PaymentBrandRegistry.Tone tone)
            => tone == PaymentBrandRegistry.Tone.DarkOnLight
                ? PaymentBrandRegistry.Tone.LightOnDark
                : PaymentBrandRegistry.Tone.DarkOnLight;

        // Stripe brand purple, applied on both sheet tones for the
        // "Pay with Stripe" lockup. Matches the USS --stripe rule.
        static readonly Color32 c_StripePurple = new Color32(0x63, 0x5B, 0xFF, 0xFF);

        Button BuildOptionButton(PurchaseOptionViewModel.PurchaseOpt opt, Sprite? brand, bool isApplePay, bool isGooglePay, bool isCoda, bool isStripe)
        {
            var theme = m_CurrentTheme;
            var isLightSheet = theme.BrandTone == PaymentBrandRegistry.Tone.DarkOnLight;
            var otherTheme = isLightSheet ? s_DarkTheme : s_LightTheme;

            Color bg;
            if ((isApplePay || isGooglePay) && brand != null)
            {
                // Apple Pay and Google Pay follow brand guidelines: pure black
                // button on light sheets, pure white on dark.
                bg = isLightSheet
                    ? (Color)new Color32(0, 0, 0, 255)
                    : (Color)new Color32(255, 255, 255, 255);
            }
            else if (isCoda && brand != null)
            {
                // CODA contrasts the sheet by borrowing the opposite theme's
                // branded color — the same palette, just flipped.
                bg = otherTheme.BrandedBg;
            }
            else if (isStripe && brand != null)
            {
                // Light sheet → purple button + white wordmark (Stripe's lockup).
                // Dark sheet → invert the swatch: white button + purple wordmark.
                bg = isLightSheet ? c_StripePurple : (Color)new Color32(255, 255, 255, 255);
            }
            else
            {
                bg = opt.IsNative && brand != null ? theme.NativeBg
                  : brand != null                  ? theme.BrandedBg
                  :                                  theme.GenericBg;
            }

            var go = new GameObject($"Pay_{opt.StoreName}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(m_Sheet, worldPositionStays: false);

            var img = go.GetComponent<Image>();
            img.color = bg;
            img.sprite = ButtonSprite;
            img.type = Image.Type.Sliced;

            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 96f;

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => { SetVisible(false); _ = m_VM!.CompleteChoice(opt); });

            // For Stripe, the "Pay with" text and the brand wordmark share
            // one color: white on light sheets (purple button), purple on dark
            // sheets (white button). The white sprite tints to purple cleanly
            // since it's white pixels with alpha.
            var stripeAccent = isLightSheet ? Color.white : (Color)c_StripePurple;

            if (opt.IsNative && brand != null)
                BuildBrandOnly(go.transform, brand);
            else if (brand != null)
                BuildPayWithBrand(go.transform, brand,
                    textColor:  isStripe ? stripeAccent
                              : isCoda   ? otherTheme.ButtonText
                              :            theme.ButtonText,
                    brandTint:  isStripe ? stripeAccent : Color.white);
            else
                BuildTextOnly(go.transform, opt.Label, theme.ButtonText);

            if (!string.IsNullOrEmpty(opt.Badge))
                ApplyBadge(go.transform, opt.Badge!);

            return btn;
        }

        // Routes the webshop choice through the VM, which fires
        // RedirectToWebshop on the PSP extension. m_DynamicButtons tracks the
        // button for theme rebuild + Dispose cleanup.
        Button BuildWebshopButton(PurchaseOptionViewModel.PurchaseOpt opt)
        {
            var theme = m_CurrentTheme;
            var glyphPath = theme.BrandTone == PaymentBrandRegistry.Tone.DarkOnLight
                ? "com.unity.purchasing/Brands/Webshop"
                : "com.unity.purchasing/Brands/WebshopWhite";
            var glyph = Resources.Load<Sprite>(glyphPath);

            var go = new GameObject("WebshopButton",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(m_Sheet, worldPositionStays: false);

            var img = go.GetComponent<Image>();
            img.color = theme.GenericBg;
            img.sprite = ButtonSprite;
            img.type = Image.Type.Sliced;

            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 96f;

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => { SetVisible(false); _ = m_VM!.CompleteChoice(opt); });

            BuildGlyphAndLabel(go.transform, glyph, "Continue with webshop", theme.ButtonText);

            if (!string.IsNullOrEmpty(opt.Badge))
                ApplyBadge(go.transform, opt.Badge!);

            return btn;
        }

        static void BuildGlyphAndLabel(Transform parent, Sprite? glyph, string label, Color textColor)
        {
            var rowGo = NewUiChild(parent, "Row", typeof(HorizontalLayoutGroup));
            StretchFull(rowGo);
            var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            if (glyph != null)
            {
                var imgGo = NewUiChild(rowGo.transform, "Glyph", typeof(Image));
                const float h = 32f;
                var aspect = (float)glyph.texture.width / glyph.texture.height;
                var imgLe = imgGo.AddComponent<LayoutElement>();
                imgLe.preferredHeight = h;
                imgLe.preferredWidth = h * aspect;
                var img = imgGo.GetComponent<Image>();
                img.sprite = glyph;
                img.preserveAspect = true;
            }

            var txt = AddText(rowGo.transform, "Label", label,
                fontSize: 28, bold: true, color: textColor, align: TextAnchor.MiddleCenter);
            var txtLe = txt.gameObject.AddComponent<LayoutElement>();
            txtLe.preferredHeight = 40f;
        }

        static void BuildBrandOnly(Transform parent, Sprite brand)
        {
            var imgGo = NewUiChild(parent, "Brand", typeof(Image));
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var aspect = (float)brand.texture.width / brand.texture.height;
            const float h = 52f;
            rt.sizeDelta = new Vector2(h * aspect, h);
            var img = imgGo.GetComponent<Image>();
            img.sprite = brand;
            img.preserveAspect = true;
        }

        static void BuildPayWithBrand(Transform parent, Sprite brand, Color textColor, Color brandTint)
        {
            var rowGo = NewUiChild(parent, "Row", typeof(HorizontalLayoutGroup));
            StretchFull(rowGo);
            var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var txt = AddText(rowGo.transform, "PayWith", "Pay with",
                fontSize: 32, bold: false, color: textColor, align: TextAnchor.MiddleCenter);
            var txtLe = txt.gameObject.AddComponent<LayoutElement>();
            txtLe.preferredHeight = 40f;

            var imgGo = NewUiChild(rowGo.transform, "Brand", typeof(Image));
            var aspect = (float)brand.texture.width / brand.texture.height;
            const float h = 40f;
            var imgLe = imgGo.AddComponent<LayoutElement>();
            imgLe.preferredHeight = h;
            imgLe.preferredWidth = h * aspect;
            var img = imgGo.GetComponent<Image>();
            img.sprite = brand;
            img.color = brandTint;
            img.preserveAspect = true;
        }

        static void BuildTextOnly(Transform parent, string label, Color textColor)
        {
            var txt = AddText(parent, "Label", label,
                fontSize: 32, bold: true, color: textColor, align: TextAnchor.MiddleCenter);
            var rt = (RectTransform)txt.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(20f, 6f);
            rt.offsetMax = new Vector2(-20f, -6f);
        }

        void ApplyBadge(Transform buttonRoot, string badgeText)
        {
            var theme = m_CurrentTheme;
            var badgeGo = new GameObject("Badge",
                typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var rt = (RectTransform)badgeGo.transform;
            rt.SetParent(buttonRoot, worldPositionStays: false);
            // Pivot at right-middle, anchored to the button's right-middle edge.
            // Positive X overflows past the button — matches the "sticker on the
            // card" look the UITK USS sets via right: -10px.
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(110f, 40f);
            rt.anchoredPosition = new Vector2(10f, 0f);

            var img = badgeGo.GetComponent<Image>();
            img.color = theme.BadgeBg;
            img.sprite = BadgeSprite;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;

            var le = badgeGo.GetComponent<LayoutElement>();
            le.ignoreLayout = true;

            var label = AddText(badgeGo.transform, "BadgeLabel", badgeText,
                fontSize: 22, bold: true, color: theme.BadgeText, align: TextAnchor.MiddleCenter);
            var labelRt = (RectTransform)label.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
        }

        void ClearDynamicButtons()
        {
            foreach (var b in m_DynamicButtons)
                if (b != null) UnityEngine.Object.Destroy(b.gameObject);
            m_DynamicButtons.Clear();
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
        static Sprite CreateRoundedSprite(int size, int radius, bool sliced)
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
            var border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
            return Sprite.Create(tex, rect, pivot, pixelsPerUnit: 100f, extrude: 0,
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
