#nullable enable
using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// Resolves brand sprites (Apple Pay, Google Pay, CODA, Stripe, ...) for
    /// the payment picker. SDK-bundled PNGs ship under
    /// Resources/com.unity.purchasing/Brands/ — the package-id subdirectory
    /// is the Unity-recommended namespace to avoid Resources collisions with
    /// other packages or user assets.
    /// Callers pick a Tone matching the button background; null return means
    /// the brand has no asset for that tone and the caller should fall back
    /// to a plain text label.
    static class PaymentBrandRegistry
    {
        public enum Tone
        {
            /// Dark-colored logo, for light backgrounds.
            DarkOnLight,
            /// Light-colored logo, for dark backgrounds.
            LightOnDark,
        }

        // One canonical key per brand. The backend owns the wire format and
        // we'll normalize on ingest if it ever drifts.
        static readonly Dictionary<string, (string? dark, string? light)> s_BrandPaths
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["AppleAppStore"] = ("com.unity.purchasing/Brands/ApplePay",  "com.unity.purchasing/Brands/ApplePayWhite"),
                ["GooglePlay"]    = ("com.unity.purchasing/Brands/GooglePay", "com.unity.purchasing/Brands/GooglePayWhite"),
                ["codapay"]       = ("com.unity.purchasing/Brands/Coda",      "com.unity.purchasing/Brands/CodaWhite"),
                ["stripe"]        = (null,                                    "com.unity.purchasing/Brands/StripeWhite"),
            };

        static readonly Dictionary<string, Sprite> s_Cache = new();

        // True when the storeName resolves to the Apple Pay brand on the
        // current (or overridden) platform. Used by the picker to invert the
        // button styling per Apple Pay brand guidelines: black-on-light-sheet,
        // white-on-dark-sheet.
        public static bool IsApplePay(string storeName)
            => ResolveBrandKey(storeName) == "AppleAppStore";

        // True when the storeName resolves to the Google Pay brand. Used by
        // the picker to render the "white with outline" variant on light
        // sheets per Google Pay brand guidelines.
        public static bool IsGooglePay(string storeName)
            => ResolveBrandKey(storeName) == "GooglePlay";

        // True when the storeName resolves to the CODA brand. The picker
        // gives CODA the "contrast the sheet" treatment: the button uses the
        // opposite theme's branded color, and the brand sprite tone inverts.
        public static bool IsCoda(string storeName)
            => string.Equals(ResolveBrandKey(storeName), "codapay", StringComparison.OrdinalIgnoreCase);

        // True when the storeName resolves to the Stripe brand. The picker
        // paints the button Stripe purple (#635BFF) on both sheet tones and
        // forces the white wordmark, matching Stripe's "Pay with Stripe"
        // lockup guideline. Only a white logo ships, so without this branch
        // light sheets would fall back to a text label.
        public static bool IsStripe(string storeName)
            => string.Equals(ResolveBrandKey(storeName), "stripe", StringComparison.OrdinalIgnoreCase);

        public static Sprite? GetBrand(string storeName, Tone tone)
        {
            var key = ResolveBrandKey(storeName);
            if (key == null) return null;
            if (!s_BrandPaths.TryGetValue(key, out var paths)) return null;

            var path = tone == Tone.DarkOnLight ? paths.dark : paths.light;
            if (string.IsNullOrEmpty(path)) return null;

            if (s_Cache.TryGetValue(path!, out var cached)) return cached;

            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return null;

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 100f);
            s_Cache[path!] = sprite;
            return sprite;
        }

        // "native" → platform-specific brand (iOS=Apple, Android=Google).
        // Editor / unknown platform → null, caller falls back to text.
        static string? ResolveBrandKey(string storeName)
        {
            if (string.Equals(storeName, IPaymentOptionProvider.NativeAlias, StringComparison.OrdinalIgnoreCase))
            {
                return GetEffectivePlatform() switch
                {
                    RuntimePlatform.IPhonePlayer => "AppleAppStore",
                    RuntimePlatform.tvOS         => "AppleAppStore",
                    RuntimePlatform.OSXPlayer    => "AppleAppStore",
                    RuntimePlatform.Android      => "GooglePlay",
                    _                            => null,
                };
            }
            return storeName;
        }

        static RuntimePlatform GetEffectivePlatform()
        {
#if UNITY_EDITOR
            if (OverridePlatform.HasValue) return OverridePlatform.Value;
#endif
            return Application.platform;
        }

#if UNITY_EDITOR
        // Editor-only override for previewing the picker's per-platform branding
        // (Apple Pay / Google Pay) without entering Sim mode. SessionState
        // survives domain reloads; clears when the Editor closes.
        const string k_OverrideKey = "PaymentBrandRegistry.OverridePlatform";
        internal static RuntimePlatform? OverridePlatform
        {
            get
            {
                var raw = UnityEditor.SessionState.GetInt(k_OverrideKey, -1);
                return raw < 0 ? (RuntimePlatform?)null : (RuntimePlatform)raw;
            }
            set
            {
                if (value.HasValue) UnityEditor.SessionState.SetInt(k_OverrideKey, (int)value.Value);
                else UnityEditor.SessionState.EraseInt(k_OverrideKey);
            }
        }
#endif
    }
}
