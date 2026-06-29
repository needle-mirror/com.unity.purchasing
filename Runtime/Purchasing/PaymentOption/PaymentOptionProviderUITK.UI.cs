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

        // BEM USS classes — single source of truth, mirror PaymentOptionProviderModal.uss.
        const string k_Block          = "unity-iap-picker";
        const string k_BlockDark      = "unity-iap-picker--dark";
        const string k_Overlay        = "unity-iap-picker__overlay";
        const string k_OverlayHidden  = "unity-iap-picker__overlay--hidden";
        const string k_Sheet          = "unity-iap-picker__sheet";
        const string k_Header         = "unity-iap-picker__header";
        const string k_Title          = "unity-iap-picker__title";
        const string k_Close          = "unity-iap-picker__close";
        const string k_Button         = "unity-iap-picker__button";
        const string k_ButtonNative   = "unity-iap-picker__button--native";
        const string k_ButtonBranded  = "unity-iap-picker__button--branded";
        const string k_ButtonGeneric  = "unity-iap-picker__button--generic";
        const string k_ButtonApplePay = "unity-iap-picker__button--apple-pay";
        const string k_ButtonGooglePay = "unity-iap-picker__button--google-pay";
        const string k_ButtonCoda     = "unity-iap-picker__button--coda";
        const string k_ButtonStripe   = "unity-iap-picker__button--stripe";
        const string k_ButtonWebshop  = "unity-iap-picker__button--webshop";
        const string k_ButtonLabel    = "unity-iap-picker__button-label";
        const string k_ButtonGlyph    = "unity-iap-picker__button-glyph";
        const string k_Brand          = "unity-iap-picker__brand";
        const string k_BrandLarge     = "unity-iap-picker__brand--large";
        const string k_Badge          = "unity-iap-picker__badge";

        IReadOnlyList<PurchaseOptionViewModel.PurchaseOpt>? m_CurrentOpts;

        void BuildHierarchy(VisualElement hostRoot, StyleSheet styleSheet)
        {
            m_Modal = new VisualElement { name = "iapPickerModal", pickingMode = PickingMode.Ignore };
            m_Modal.AddToClassList(k_Block);
            m_Modal.styleSheets.Add(styleSheet);
            hostRoot.Add(m_Modal);
            m_Modal.BringToFront();

            m_Overlay = new VisualElement { name = "purchaseOverlay" };
            m_Overlay.AddToClassList(k_Overlay);
            m_Modal.Add(m_Overlay);

            m_Sheet = new VisualElement { name = "purchaseSheet" };
            m_Sheet.AddToClassList(k_Sheet);
            m_Overlay.Add(m_Sheet);

            var header = new VisualElement { name = "header" };
            header.AddToClassList(k_Header);
            m_Sheet.Add(header);

            var title = new Label("How would you like to pay?");
            title.AddToClassList(k_Title);
            header.Add(title);

            m_CloseX = new Button { name = "closeX", text = "×" };
            m_CloseX.AddToClassList(k_Close);
            header.Add(m_CloseX);
        }

        void BuildButtons(IReadOnlyList<PurchaseOptionViewModel.PurchaseOpt> opts)
        {
            m_CurrentOpts = opts;
            ClearDynamicButtons();
            if (m_Sheet == null) return;

            var sheetTone = m_DarkMode
                ? PaymentBrandRegistry.Tone.LightOnDark
                : PaymentBrandRegistry.Tone.DarkOnLight;

            for (var i = 0; i < opts.Count; i++)
            {
                var opt = opts[i];

                if (opt.IsWebshop)
                {
                    var webshopBtn = BuildWebshopButton(opt);
                    m_Sheet.Add(webshopBtn);
                    m_DynamicButtons.Add(webshopBtn);
                    continue;
                }

                var isApplePay = PaymentBrandRegistry.IsApplePay(opt.StoreName);
                var isGooglePay = PaymentBrandRegistry.IsGooglePay(opt.StoreName);
                var isCoda = PaymentBrandRegistry.IsCoda(opt.StoreName);
                var isStripe = PaymentBrandRegistry.IsStripe(opt.StoreName);
                // Apple Pay, Google Pay and CODA: button color contrasts the
                // sheet, so the brand sprite must invert too. Stripe: always the
                // purple wordmark lockup, so force the white logo regardless of
                // sheet tone (USS paints the purple background).
                var tone = isApplePay || isGooglePay || isCoda ? Invert(sheetTone)
                    : isStripe ? PaymentBrandRegistry.Tone.LightOnDark
                    : sheetTone;
                var brand = PaymentBrandRegistry.GetBrand(opt.StoreName, tone);
                var btn = BuildOptionButton(i, opt, brand, isApplePay, isGooglePay, isCoda, isStripe);
                m_Sheet.Add(btn);
                m_DynamicButtons.Add(btn);
            }
        }

        // Routes the webshop choice through the VM, which fires
        // RedirectToWebshop on the PSP extension. Tracked in m_DynamicButtons
        // for theme rebuild + cleanup.
        Button BuildWebshopButton(PurchaseOptionViewModel.PurchaseOpt opt)
        {
            var glyphPath = m_DarkMode ? "com.unity.purchasing/Brands/WebshopWhite" : "com.unity.purchasing/Brands/Webshop";
            var glyph = Resources.Load<Sprite>(glyphPath);

            var btn = new Button { name = "webshopButton", text = "" };
            btn.AddToClassList(k_Button);
            btn.AddToClassList(k_ButtonGeneric);
            btn.AddToClassList(k_ButtonWebshop);

            if (glyph != null)
            {
                var img = new Image { sprite = glyph, scaleMode = ScaleMode.ScaleToFit };
                img.AddToClassList(k_ButtonGlyph);
                var aspect = (float)glyph.texture.width / glyph.texture.height;
                img.style.width = 32f * aspect;
                btn.Add(img);
            }

            var label = new Label("Continue with webshop");
            label.AddToClassList(k_ButtonLabel);
            btn.Add(label);

            if (!string.IsNullOrEmpty(opt.Badge))
            {
                var badgeLabel = new Label(opt.Badge);
                badgeLabel.AddToClassList(k_Badge);
                badgeLabel.pickingMode = PickingMode.Ignore;
                btn.Add(badgeLabel);
            }

            btn.clicked += () => { m_Overlay!.AddToClassList(k_OverlayHidden); _ = m_VM!.CompleteChoice(opt); };
            return btn;
        }

        static PaymentBrandRegistry.Tone Invert(PaymentBrandRegistry.Tone tone)
            => tone == PaymentBrandRegistry.Tone.DarkOnLight
                ? PaymentBrandRegistry.Tone.LightOnDark
                : PaymentBrandRegistry.Tone.DarkOnLight;

        Button BuildOptionButton(int index, PurchaseOptionViewModel.PurchaseOpt opt, Sprite? brand, bool isApplePay, bool isGooglePay, bool isCoda, bool isStripe)
        {
            var nativeWithBrand = opt.IsNative && brand != null;

            var btn = new Button { name = $"pay_{index}", text = "" };
            btn.AddToClassList(k_Button);
            if (isApplePay && brand != null)
            {
                btn.AddToClassList(k_ButtonApplePay);
            }
            else if (isGooglePay && brand != null)
            {
                btn.AddToClassList(k_ButtonGooglePay);
            }
            else if (isCoda && brand != null)
            {
                btn.AddToClassList(k_ButtonCoda);
            }
            else if (isStripe && brand != null)
            {
                btn.AddToClassList(k_ButtonStripe);
            }
            else
            {
                btn.AddToClassList(
                    nativeWithBrand ? k_ButtonNative
                    : brand != null ? k_ButtonBranded
                    :                 k_ButtonGeneric);
            }

            if (nativeWithBrand)
            {
                btn.Add(BuildBrand(brand!, large: true, tint: null));
            }
            else if (brand != null)
            {
                var label = new Label("Pay with");
                label.AddToClassList(k_ButtonLabel);
                btn.Add(label);
                // Stripe ships only the white wordmark — tint it purple on dark
                // sheets so the inverted (white-button) variant reads correctly.
                Color? brandTint = isStripe && m_DarkMode
                    ? new Color(99f / 255f, 91f / 255f, 1f, 1f)
                    : (Color?)null;
                btn.Add(BuildBrand(brand, large: false, tint: brandTint));
            }
            else
            {
                var label = new Label(opt.Label);
                label.AddToClassList(k_ButtonLabel);
                btn.Add(label);
            }

            if (!string.IsNullOrEmpty(opt.Badge))
            {
                var badge = new Label(opt.Badge);
                badge.AddToClassList(k_Badge);
                badge.pickingMode = PickingMode.Ignore;
                btn.Add(badge);
            }

            btn.clicked += () => { m_Overlay!.AddToClassList(k_OverlayHidden); _ = m_VM!.CompleteChoice(opt); };
            return btn;
        }

        static Image BuildBrand(Sprite sprite, bool large, Color? tint)
        {
            var img = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit };
            img.AddToClassList(k_Brand);
            if (large) img.AddToClassList(k_BrandLarge);
            if (tint.HasValue) img.tintColor = tint.Value;
            var aspect = (float)sprite.texture.width / sprite.texture.height;
            var h = large ? 52f : 40f;
            img.style.width = h * aspect;
            return img;
        }

        void ClearDynamicButtons()
        {
            foreach (var b in m_DynamicButtons)
                b?.RemoveFromHierarchy();
            m_DynamicButtons.Clear();
        }
    }
}

#endif
