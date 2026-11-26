#nullable enable

namespace UnityEngine.Purchasing
{
    // Simple POP spec colors, the counterpart of PaymentOptionPopMetrics for color. The
    // UGUI provider paints from these directly. The UITK provider styles itself through
    // PaymentOptionProviderModal.uss so that consumers keep overriding its --color-*
    // variables; the values there must match these, which PaymentOptionProviderUITKPanelTests
    // checks for the modal's own colors.
    //
    // Every value is the spec's Colors table verbatim unless its comment says otherwise.
    internal static class PaymentOptionPopPalette
    {
        // Same in both themes.
        internal static readonly Color32 Scrim = new Color32(0, 0, 0, 153); // #000000 @ 60%

        internal readonly struct Colors
        {
            public readonly Color32 PopBackground;
            public readonly Color32 PopBorder;
            public readonly Color32 HeaderText;
            public readonly Color32 CancelButton;
            // Text on the theme-colored buttons (webshop, and the generic text-only button).
            public readonly Color32 ButtonText;
            public readonly Color32 BadgeBackground;
            public readonly Color32 BadgeText;

            public Colors(Color32 popBackground, Color32 popBorder, Color32 headerText, Color32 cancelButton,
                Color32 buttonText, Color32 badgeBackground, Color32 badgeText)
            {
                PopBackground = popBackground;
                PopBorder = popBorder;
                HeaderText = headerText;
                CancelButton = cancelButton;
                ButtonText = buttonText;
                BadgeBackground = badgeBackground;
                BadgeText = badgeText;
            }
        }

        internal static readonly Colors Light = new Colors(
            popBackground:   new Color32(255, 255, 255, 255), // #FFFFFF
            popBorder:       new Color32(187, 187, 187, 255), // #BBBBBB
            headerText:      new Color32(32,  32,  32,  255), // #202020
            cancelButton:    new Color32(131, 131, 131, 255), // #838383
            buttonText:      new Color32(0,   0,   0,   255), // #000000 webshop text
            badgeBackground: new Color32(196, 232, 209, 255), // #C4E8D1
            badgeText:       new Color32(25,  59,  45,  255)); // #193B2D

        internal static readonly Colors Dark = new Colors(
            popBackground:   new Color32(42,  42,  42,  255), // #2A2A2A
            popBorder:       new Color32(58,  58,  58,  255), // #3A3A3A
            headerText:      new Color32(238, 238, 238, 255), // #EEEEEE
            cancelButton:    new Color32(123, 123, 123, 255), // #7B7B7B
            buttonText:      new Color32(255, 255, 255, 255), // #FFFFFF webshop text
            badgeBackground: new Color32(23,  73,  51,  255), // #174933
            badgeText:       new Color32(177, 241, 203, 255)); // #B1F1CB

        internal static Colors For(bool dark) => dark ? Dark : Light;

        static readonly Color32 k_Black = new Color32(0, 0, 0, 255);
        static readonly Color32 k_White = new Color32(255, 255, 255, 255);
        static readonly Color32 k_Gray  = new Color32(135, 135, 135, 255); // #878787

        // Button background by the ViewModel's button kind (the descriptor's colors key).
        // Brand buttons keep their lockup colors; only the native stores' flip with the theme.
        internal static Color32 ButtonBackground(PurchaseOptionViewModel.ButtonKind kind, bool dark)
        {
            switch (kind)
            {
                case PurchaseOptionViewModel.ButtonKind.Apple:
                // The native store without a lockup (the fake store) is drawn in the Apple
                // native colors, as in the mocks.
                case PurchaseOptionViewModel.ButtonKind.NativeText:
                // Not the spec's #202124 on the dark theme: Google Play follows the Apple button,
                // black on the light theme and white on the dark one (#202124 all but vanished on
                // the dark POP's #2A2A2A), with the dark wordmark (LogoTone).
                case PurchaseOptionViewModel.ButtonKind.GooglePlay:
                    return dark ? k_White : k_Black;                                            // #FFFFFF | #000000
                case PurchaseOptionViewModel.ButtonKind.Coda:
                // Not in the spec's table: the mocks draw the mock PSP in Coda's gray.
                case PurchaseOptionViewModel.ButtonKind.MockProvider:
                    return k_Gray;                                                               // #878787
                case PurchaseOptionViewModel.ButtonKind.Stripe:
                    return new Color32(82, 58, 253, 255);                                       // #523AFD
                default:
                    // Webshop per the spec. The generic button (a store with no brand, or
                    // Branded: a logo without a treatment of its own) shares it; the spec has
                    // no row for that one.
                    return dark ? new Color32(56, 56, 56, 255) : new Color32(240, 240, 240, 255); // #383838 | #F0F0F0
            }
        }

        // Text color by button kind: always the one that contrasts ButtonBackground, so a
        // label is readable whatever the button. Webshop / generic / branded follow the
        // theme (the spec's webshop text); brand and native buttons have their own.
        internal static Color32 ButtonText(PurchaseOptionViewModel.ButtonKind kind, bool dark)
        {
            switch (kind)
            {
                case PurchaseOptionViewModel.ButtonKind.Apple:
                case PurchaseOptionViewModel.ButtonKind.NativeText:
                case PurchaseOptionViewModel.ButtonKind.GooglePlay:
                    return dark ? k_Black : k_White;                                            // #000000 | #FFFFFF
                case PurchaseOptionViewModel.ButtonKind.Coda:
                case PurchaseOptionViewModel.ButtonKind.MockProvider:
                case PurchaseOptionViewModel.ButtonKind.Stripe:
                    return k_White;
                default:
                    return For(dark).ButtonText;
            }
        }

        // Which variant of a brand logo the button draws: the light one on a dark
        // background and the other way round. Apple and Google Play flip with the theme;
        // Coda and Stripe sit on dark brand colors in both (only a white Stripe wordmark
        // ships), and Branded sits on the generic button, which follows the theme.
        internal static PaymentBrandRegistry.Tone LogoTone(PurchaseOptionViewModel.ButtonKind kind, bool dark)
        {
            switch (kind)
            {
                case PurchaseOptionViewModel.ButtonKind.Apple:
                case PurchaseOptionViewModel.ButtonKind.GooglePlay:
                    return dark ? PaymentBrandRegistry.Tone.DarkOnLight : PaymentBrandRegistry.Tone.LightOnDark;
                case PurchaseOptionViewModel.ButtonKind.Coda:
                case PurchaseOptionViewModel.ButtonKind.Stripe:
                    return PaymentBrandRegistry.Tone.LightOnDark;
                default:
                    return dark ? PaymentBrandRegistry.Tone.LightOnDark : PaymentBrandRegistry.Tone.DarkOnLight;
            }
        }

        // The logo variant for a button drawn in these colors, which may be a consumer's (UITK
        // USS overrides): the light logo when the text is lighter than the background, the dark
        // one otherwise, so the logo reads like the text beside it. Going by the text rather than
        // by the background alone is what keeps the white logo on Coda's mid gray, where black
        // would contrast more. On the palette's own colors this is LogoTone(kind, dark) for every
        // kind with a logo, in both themes (PaymentOptionPopPaletteTests). Text as light as the
        // background can't be read anyway; then the background alone decides. Alpha is ignored
        // (the UITK provider keeps the variant on a mostly transparent button).
        internal static PaymentBrandRegistry.Tone LogoTone(Color background, Color text)
        {
            var bg = RelativeLuminance(background);
            var fg = RelativeLuminance(text);
            var lightLogo = Mathf.Approximately(fg, bg) ? bg < 0.5f : fg > bg;
            return lightLogo ? PaymentBrandRegistry.Tone.LightOnDark : PaymentBrandRegistry.Tone.DarkOnLight;
        }

        // WCAG 2 relative luminance of a color given in sRGB (as USS and Color32 values are).
        internal static float RelativeLuminance(Color c)
        {
            static float Channel(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * Channel(c.r) + 0.7152f * Channel(c.g) + 0.0722f * Channel(c.b);
        }
    }
}
