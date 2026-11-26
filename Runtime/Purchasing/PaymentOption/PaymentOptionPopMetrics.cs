#nullable enable

namespace UnityEngine.Purchasing
{
    // Simple POP spec metrics and the scale arithmetic built on them, shared by the UGUI
    // and UITK providers so both resolve the same pt -> pixel factor from the same box
    // model. Each provider applies the factor its own way (CanvasScaler.scaleFactor vs
    // PanelSettings.scale) and supplies its own screen metrics; nothing here touches a
    // UI framework or reads Screen.*, so it compiles without UI Toolkit and can be
    // tested without a device.
    //
    // Every value is the spec's pt value verbatim: the providers make one UI unit one pt
    // (iOS point / Android dp), so the numbers are not converted anywhere.
    internal static class PaymentOptionPopMetrics
    {
        internal const float SideMargin           = 16f;  // POP -> screen edge, both sides
        internal const float BottomAnchoredMargin = 48f;  // POP -> screen bottom, bottom-anchored layout only
        // The least room above and below the centered POP, so on a short screen it shrinks
        // (ResolveScaleFactor, given ReservedHeight) rather than touch the screen's (or safe
        // area's) edges.
        internal const float CenteredVerticalMargin = 16f;
        internal const float MinWidth             = 340f;
        internal const float Padding              = 20f;  // POP -> content, all four sides
        internal const float HeaderGap            = 20f;  // header text -> button box
        internal const float ButtonHeight         = 48f;
        internal const float ButtonGap            = 12f;
        internal const float ButtonCornerRadius   = 6f;
        internal const int   ButtonFontSize       = 15;   // semibold in the spec
        // Apple's In-App Purchase button only: "SF Pro 17pt/20, medium" in the spec.
        internal const int   AppleButtonFontSize  = 17;

        // Whether the platform has the OS system font (SF) the Apple button's lead text is drawn
        // in: Apple players and the macOS Editor. Each provider loads it its own way.
        internal static bool HasAppleSystemFont(RuntimePlatform platform)
            => platform == RuntimePlatform.IPhonePlayer
            || platform == RuntimePlatform.tvOS
            || platform == RuntimePlatform.OSXPlayer
            || platform == RuntimePlatform.OSXEditor
#if UNITY_VISIONOS
            || platform == RuntimePlatform.VisionOS
#endif
            ;
        // The Apple button's lead text face: the system font's Medium (Apple's guidelines for the
        // In-App Purchase mark: SF Pro Medium). The system font is one variable font file
        // (SFNS.ttf on macOS, SFUI.ttf on iOS) whose family is "System Font" (macOS 27, iOS 27;
        // ".SF NS" and the like on older systems, see IsAppleSystemFontFamily) and whose weights
        // are named instances; looked up by family and style, TextCore and legacy Fonts only find
        // its default instance, Regular. FreeType opens a named instance as face index
        // (instance << 16), which TextCore's (and TextMesh Pro's) font assets pass through.
        internal const string AppleSystemFontFamily = "System Font";
        internal const string AppleSystemFontWeight = "Medium";
        // Point size font assets sample its glyphs at (TextCore's default for OS fonts), and the
        // padding and atlas size of the asset both providers build from it, so the two can't
        // render the label differently.
        internal const int AppleSystemFontSamplingSize = 90;
        internal const int AppleSystemFontPadding = 9;
        internal const int AppleSystemFontAtlasSize = 1024;

        // The system font's family name: "System Font" on current systems; older ones name it
        // with a leading dot (".SF NS" on macOS 13), a name hidden from font menus.
        internal static bool IsAppleSystemFontFamily(string? family)
            => family == AppleSystemFontFamily
            || (family != null && family.StartsWith(".SF", System.StringComparison.Ordinal));

        // The system font's file: SFNS.ttf on macOS, SFUI.ttf on iOS. Matched by name because the
        // other SF* files (SF Compact, SF Arabic, SF NS Mono, ...) carry ".SF" family names too
        // and have their own Medium instances, at other widths.
        internal static bool IsAppleSystemFontFile(string path)
        {
            var name = System.IO.Path.GetFileName(path);
            return string.Equals(name, "SFNS.ttf", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "SFUI.ttf", System.StringComparison.OrdinalIgnoreCase);
        }

        // The font file and face index of the system font's Medium face: the OS font file
        // IsAppleSystemFontFile matches, if its face 0 is the system font (IsAppleSystemFontFamily),
        // at its face or named instance styled "Medium". Looked up once per domain (it opens a few font files); false where there is
        // none. Uses the TextCore font engine, not a UI framework, for both providers.
        internal static bool TryFindAppleSystemFontMedium(out string path, out int faceIndex)
        {
            if (!s_AppleMediumSearched)
            {
                s_AppleMediumSearched = true;
                try
                {
                    foreach (var candidate in Font.GetPathsToOSFonts())
                    {
                        if (!IsAppleSystemFontFile(candidate))
                            continue;
                        var index = FindFace(candidate, AppleSystemFontWeight);
                        if (index < 0)
                            continue;
                        s_AppleMediumPath = candidate;
                        s_AppleMediumFaceIndex = index;
                        break;
                    }
                    Debug.unityLogger.LogIAPVerbose(s_AppleMediumPath != null
                        ? $"AppleSystemFont: {AppleSystemFontWeight} is {System.IO.Path.GetFileName(s_AppleMediumPath)}, face {s_AppleMediumFaceIndex} (named instance {s_AppleMediumFaceIndex >> 16})"
                        : $"AppleSystemFont: no {AppleSystemFontWeight} face in the system font file");
                }
                catch (System.Exception e)
                {
                    Debug.unityLogger.LogIAPVerbose($"AppleSystemFont: looking for {AppleSystemFontFamily} {AppleSystemFontWeight} failed: {e.Message}");
                }
            }

            path = s_AppleMediumPath ?? "";
            faceIndex = s_AppleMediumFaceIndex;
            return s_AppleMediumPath != null;
        }

        // TextCore's CreateFontAsset(path, faceIndex, ...) reads the new asset's definition before
        // it stores the path, so its first face load has no file to go to and logs "Unable to load
        // font face for [<name>] font asset." — for any font file, an ordinary one at face 0 too
        // (6000.3 and 7000.0; the name is empty on 6000.3). The asset then loads from the file and
        // renders correctly. Drops that one warning while the asset is created: only the asset being
        // created can log it inside the call. Every font asset the providers create from an OS font
        // goes through it (TextCore's by path and by family, TextMesh Pro's by path, which builds on
        // TextCore), so the Apple button's font logs nothing outside verbose IAP logging. Every other log, errors and exceptions included, goes
        // through unchanged, and the previous handler is back afterwards even if creation throws.
        internal sealed class DropFontFaceWarning : ILogHandler
        {
            const string k_Start = "Unable to load font face for [";
            const string k_End = "] font asset.";
            readonly ILogHandler m_Inner;

            DropFontFaceWarning(ILogHandler inner) => m_Inner = inner;

            public void LogFormat(LogType logType, Object? context, string format, params object[] args)
            {
                if (logType == LogType.Warning && args.Length == 1 && args[0] is string message
                    && message.StartsWith(k_Start, System.StringComparison.Ordinal)
                    && message.EndsWith(k_End, System.StringComparison.Ordinal))
                    return;
                m_Inner.LogFormat(logType, context, format, args);
            }

            public void LogException(System.Exception exception, Object? context)
                => m_Inner.LogException(exception, context);

            internal static T Around<T>(System.Func<T> create)
            {
                var logger = Debug.unityLogger;
                var inner = logger.logHandler;
                logger.logHandler = new DropFontFaceWarning(inner);
                try
                {
                    return create();
                }
                finally
                {
                    logger.logHandler = inner;
                }
            }
        }

        // Whether the system font's Medium face (TryFindAppleSystemFontMedium) has a glyph for every
        // character of text, so the providers only draw text in it that it can draw: the SF file
        // has Latin, Greek and Cyrillic but no Hangul, CJK or Arabic, which the OS draws from other
        // fonts, and a font asset built from the file alone would show empty boxes for them (a
        // translation in such a script). Spaces don't count. Cached per text.
        internal static bool AppleSystemFontCanDraw(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;
            if (s_AppleCanDraw.TryGetValue(text, out var cached))
                return cached;
            // No Medium file to check against (the providers then draw by family name, if at all):
            // as before, the font is used.
            if (!TryFindAppleSystemFontMedium(out var path, out var faceIndex))
                return true;
            var canDraw = false;
            try
            {
                if (TextCore.LowLevel.FontEngine.LoadFontFace(path, AppleSystemFontSamplingSize, faceIndex)
                    == TextCore.LowLevel.FontEngineError.Success)
                {
                    canDraw = true;
                    for (var i = 0; i < text.Length && canDraw; i++)
                    {
                        if (char.IsWhiteSpace(text[i]))
                            continue;
                        var unicode = char.IsSurrogatePair(text, i) ? char.ConvertToUtf32(text, i++) : text[i];
                        canDraw = TextCore.LowLevel.FontEngine.TryGetGlyphWithUnicodeValue((uint)unicode,
                            TextCore.LowLevel.GlyphLoadFlags.LOAD_NO_BITMAP, out var glyph) && glyph.index != 0;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.unityLogger.LogIAPVerbose($"AppleSystemFont: checking its glyphs failed: {e.Message}");
                canDraw = false;
            }
            if (!canDraw)
                Debug.unityLogger.LogIAPVerbose($"AppleSystemFont: \"{text}\" has characters the system font's {AppleSystemFontWeight} face lacks; drawn in the button's other font");
            s_AppleCanDraw[text] = canDraw;
            return canDraw;
        }

        static readonly System.Collections.Generic.Dictionary<string, bool> s_AppleCanDraw = new();

        static bool s_AppleMediumSearched;
        static string? s_AppleMediumPath;
        static int s_AppleMediumFaceIndex;

        // The FreeType face index of the system font's style in the font file at path, or -1: face
        // 0, or a variable font's named instance (instance << 16; FreeType numbers them from 1,
        // and fails past the last). Only in a file whose face 0 is the system font.
        internal static int FindFace(string path, string style)
        {
            // SFNS.ttf has hundreds of named instances (width x weight x grade), in an order that
            // varies by macOS version: Medium is instance 5 on macOS 27, past the compressed and
            // condensed widths on older systems. 0x7FFF is FreeType's limit on the instance index.
            const int maxInstances = 0x7FFF;
            for (var instance = 0; instance < maxInstances; instance++)
            {
                var faceIndex = instance << 16;
                if (TextCore.LowLevel.FontEngine.LoadFontFace(path, AppleSystemFontSamplingSize, faceIndex)
                    != TextCore.LowLevel.FontEngineError.Success)
                    return -1;
                var face = TextCore.LowLevel.FontEngine.GetFaceInfo();
                if (!IsAppleSystemFontFamily(face.familyName))
                    return -1;
                if (face.styleName == style)
                    return faceIndex;
            }
            return -1;
        }

        // Not in the spec: the least room between a button's edge and its content, 10pt each
        // side. Brand logos: ButtonLogoHeight / ButtonLogoGap below.
        internal const float ButtonSidePadding    = 10f;

        // The badge ("Bonus Reward Chip / Discount Chip" in the spec): text only, 12pt / 16
        // semibold, 20pt tall, as wide as its text plus 6pt each side, a pill (the spec's
        // corner radius 100 is "fully round", i.e. half the height), vertically centered,
        // 20pt from the button's right edge.
        internal const float BadgeHeight          = 20f;
        internal const float BadgePadding         = 6f;   // inside the pill, left and right
        internal const float BadgeCornerRadius    = BadgeHeight / 2f;
        internal const int   BadgeFontSize        = 12;
        // Documentary: the spec's line height. Not applied (UI Toolkit has no line-height);
        // a single 12pt line is centered in the 20pt pill.
        internal const float BadgeLineHeight      = 16f;
        internal const float BadgeRightOffset     = 20f;  // badge -> button's right edge
        // Not in the spec. The least room between the button's content and the badge.
        internal const float ButtonBadgeGap       = 8f;
        // Not in the spec. The badge has no fixed cap: it takes the room its button's content
        // leaves and is ellipsized only when it doesn't fit there (BadgeRoom), but never below
        // BadgeMinWidth (or its own width, if narrower), so a short badge ("Best value") always
        // shows whole. Above that the content comes first: a text-only label is
        // shown whole; beside a logo, "Continue with" is dropped first (FitsLeadText), then the
        // badge gives way, and a lone logo narrows only if even the least badge has no room. The
        // one exception: when even the least badge doesn't fit beside a whole text-only label
        // (a long label on a narrow POP, or a consumer stylesheet's badge min-width or padding),
        // the label ellipsizes rather than run under the badge.
        //
        // The least badge, pill included. Not in the spec, which sets no minimum: the fixed
        // badge box UGUI drew before the spec gave its text 72pt (no inner padding), and this is
        // the same 72pt of text room plus the spec's 6pt padding each side, so a badge that
        // showed whole there ("Best value", 76pt here) still does.
        internal const float BadgeMinWidth        = 72f + 2f * BadgePadding;

        // The narrowest a badge is drawn: BadgeMinWidth (84pt), or its own width if narrower, never
        // below its padding around an ellipsis (ellipsisWidth, pill included) and never below a
        // consumer's min-width.
        internal static float BadgeLeast(float naturalWidth, float ellipsisWidth, float minWidth = 0f)
            => Mathf.Max(Mathf.Min(BadgeMinWidth, naturalWidth), ellipsisWidth, minWidth);

        internal const float CornerRadius         = 12f;  // POP, all four corners
        internal const float BorderWidth          = 1f;   // drawn inside the POP's edge, within Padding
        internal const int   HeaderFontSize       = 18;
        internal const float HeaderLineHeight     = 28f;  // header text is 18pt / 28

        // Widths. Bottom-anchored fills the space between the side margins, MinWidth up
        // to BottomAnchoredMaxWidth. Centered starts at CenteredDefaultWidth and grows
        // with wider content up to CenteredMaxWidth; both narrow to the space between the
        // side margins on small screens, never below MinWidth (see PopWidth).
        internal const float BottomAnchoredMaxWidth = 412f;
        internal const float CenteredDefaultWidth   = 375f;
        internal const float CenteredMaxWidth       = 500f;

        // The cancel button's visible area is 32pt and drives the header row height; its
        // 44pt hit target (the platform minimum touch target) overflows the row rather
        // than growing it. The glyph is drawn about 16pt across.
        internal const float CancelButtonSize = 32f;
        internal const float CancelHitSize    = 44f;
        internal const int   CancelGlyphSize  = 16;

        // 1 UI unit == 1pt. Android's dp baseline (160 dpi) is what Screen.dpi maps
        // onto cleanly. Apple platforms don't use it: see AppleNativeScale.
        internal const float DpiBaseline = 160f;

        // Apple's point is not dpi/160: a device renders at a whole native scale (UIScreen.scale)
        // chosen per display, and Unity has no API for that scale. Screen.dpi reports the
        // panel's physical density, which sorts devices into the same buckets with wide gaps:
        //   1x: non-Retina iPhone/iPad 132-163, non-Retina Mac displays ~70-135
        //   2x: Retina Mac 218-254, Retina iPad 264, iPad mini/iPhone (Retina, non-Plus) 326
        //   3x: iPhone Plus 401, iPhone X and later 458-476
        // Each threshold sits mid-gap (163..218 and 326..401), so it doesn't need to be exact.
        // ponytail: a Mac display the user runs in a HiDPI mode below 200 dpi (a 27" 4K
        // monitor is 163) gets 1x, so the POP is half its point size there.
        internal const float AppleTwoXMinDpi   = 200f;
        internal const float AppleThreeXMinDpi = 360f;

        // Floor for the fit-driven shrink in ResolveScaleFactor. ponytail: a window small
        // enough to hit this floor can still clip the POP, but nothing is legible at that
        // size anyway — and a zero or negative factor would collapse the whole canvas/panel.
        internal const float MinScaleFactor = 0.3f;

        // ponytail: Screen.dpi is 0 on some Android devices and reports the desktop
        // monitor's DPI in the Editor, so the ratio is clamped rather than trusted.
        // The platform is passed in rather than read from Application.platform so every
        // branch can be tested in the Editor. The Editor itself takes the dpi/160 branch,
        // macOS included: its Game view has no native scale to match.
        internal static float PointScale(float dpi, RuntimePlatform platform)
        {
            if (dpi <= 0f) return 2f;
            return IsApplePlayer(platform)
                ? AppleNativeScale(dpi)
                : Mathf.Clamp(dpi / DpiBaseline, 1f, 4f);
        }

        // UIScreen.scale (or the Mac backing scale factor) from the panel density. dpi/160
        // would make the POP ~17.5% too small on a 264-dpi iPad (1.65 instead of 2) and ~4%
        // too small on a 460-dpi iPhone (2.875 instead of 3).
        internal static float AppleNativeScale(float dpi)
            => dpi < AppleTwoXMinDpi ? 1f : dpi < AppleThreeXMinDpi ? 2f : 3f;

        // VisionOS is guarded like everywhere else in the package: the enum value is newer
        // than the oldest supported 2022.3 editor.
        internal static bool IsApplePlayer(RuntimePlatform platform)
            => platform == RuntimePlatform.IPhonePlayer
            || platform == RuntimePlatform.tvOS
#if UNITY_VISIONOS
            || platform == RuntimePlatform.VisionOS
#endif
            || platform == RuntimePlatform.OSXPlayer;

        // One unit is one pt (PointScale), except when that would make the POP wider
        // or taller than the screen — then everything shrinks together by the same
        // factor. Scaling the whole canvas/panel rather than clamping individual widths
        // is what keeps the proportions, so a small window gets a smaller POP rather
        // than a squashed or clipped one.
        //
        // Takes the screen metrics rather than reading Screen.* so the arithmetic can
        // be exercised without one; each provider supplies the real ones.
        internal static float ResolveScaleFactor(float dpi, RuntimePlatform platform,
            int screenWidth, int screenHeight, int buttonCount, float bottomMargin)
        {
            var scale = PointScale(dpi, platform);
            var requiredWidth = MinWidth + 2f * SideMargin;
            var requiredHeight = RequiredPopHeight(buttonCount, bottomMargin);
            if (screenWidth > 0 && screenHeight > 0)
                scale = Mathf.Min(scale,
                    screenWidth / requiredWidth,
                    screenHeight / requiredHeight);
            return Mathf.Max(scale, MinScaleFactor);
        }

        // The POP's width in pt, given the space between the side margins and the width
        // its content (header row, widest button, plus the padding) asks for. Each
        // provider lays this out its own way — UGUI through its layout groups, UITK
        // through USS — so this is the reference both are tested against.
        internal static float PopWidth(bool centered, float availableWidth, float contentWidth)
        {
            var preferred = centered ? CenteredPreferredWidth(contentWidth) : BottomAnchoredMaxWidth;
            return Mathf.Max(MinWidth, Mathf.Min(availableWidth, preferred));
        }

        // Centered only: the width the POP asks for before the screen has a say.
        internal static float CenteredPreferredWidth(float contentWidth)
            => Mathf.Clamp(contentWidth, CenteredDefaultWidth, CenteredMaxWidth);

        // Height the POP needs at one unit per pt, including the gap it keeps from
        // the screen edge. Derived from the spec's box model rather than measured,
        // because a measured height would depend on the scale this very method
        // feeds — and it has to be known before the first layout pass.
        internal static float RequiredPopHeight(int buttonCount, float bottomMargin)
        {
            var buttons = Mathf.Max(1, buttonCount);
            return Padding * 2f
                 + CancelButtonSize
                 + HeaderGap
                 + buttons * ButtonHeight
                 + (buttons - 1) * ButtonGap
                 + bottomMargin;
        }

        // The vertical room RequiredPopHeight reserves beyond the POP itself, its bottomMargin.
        // Bottom-anchored, the 48pt bottom margin; centered, the CenteredVerticalMargin above
        // and below it.
        internal static float ReservedHeight(bool centered)
            => centered ? 2f * CenteredVerticalMargin : BottomAnchoredMargin;

        // The automatic layout's device rules, the UI spec's form factor rules, used
        // until the consumer picks one with ApplyBottomAnchoredLayout / ApplyCenteredLayout. Phones,
        // iPhone included, are bottom-anchored in either orientation, which is left to the game;
        // tablets and Mac are centered. Off Apple a tablet is a screen whose shorter
        // side is at least TabletMinShortSide pt (Android's large-screen threshold, 600dp): the
        // whole screen, not the safe area, so a rotation never changes the answer.
        internal static bool AutomaticCentered(RuntimePlatform platform, string deviceModel, float dpi, int width, int height)
            => platform switch
            {
                RuntimePlatform.IPhonePlayer => IsIPad(deviceModel),
                RuntimePlatform.OSXPlayer => true,
                // Screen.dpi in the Editor is the desktop monitor's, which says nothing about the
                // device a Game view previews, so its shape decides: portrait is a phone.
                RuntimePlatform.OSXEditor or RuntimePlatform.WindowsEditor or RuntimePlatform.LinuxEditor => width > height,
                _ => Mathf.Min(width, height) / PointScale(dpi, platform) >= TabletMinShortSide,
            };

        internal const float TabletMinShortSide = 600f;

        // SystemInfo.deviceModel on iOS: "iPhone14,5", "iPad13,1"; the simulator reports the same.
        // Known mismatches: an iPhone app on an Apple-silicon Mac reports a Mac model (so it gets the
        // iPhone rules), and an iPhone-only app in compatibility mode on an iPad reports "iPad..." (so
        // it is centered inside a phone-sized window).
        static bool IsIPad(string deviceModel) => deviceModel.StartsWith("iPad", System.StringComparison.Ordinal);

        // Screen.safeArea (pixels, origin bottom-left) kept within a width x height
        // screen. One that is empty or entirely off screen reads as the whole screen: there is
        // nothing to inset against, and a zero-sized room would floor the scale.
        internal static Rect ClampSafeArea(Rect safeArea, int width, int height)
        {
            var xMin = Mathf.Clamp(safeArea.xMin, 0f, width);
            var xMax = Mathf.Clamp(safeArea.xMax, 0f, width);
            var yMin = Mathf.Clamp(safeArea.yMin, 0f, height);
            var yMax = Mathf.Clamp(safeArea.yMax, 0f, height);
            if (xMax - xMin < 1f || yMax - yMin < 1f)
                return new Rect(0f, 0f, width, height);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        // How far the safe area's edges are from the screen's, in UI units (pt at
        // the given scale), for the margins to be measured from. safeArea is ClampSafeArea's.
        // Screen.safeArea has its origin bottom-left, the panel top-left: bottom is safeArea.yMin.
        internal static void SafeAreaInsets(Rect safeArea, int width, int height, float scale,
            out float left, out float right, out float top, out float bottom)
        {
            left = safeArea.xMin / scale;
            right = (width - safeArea.xMax) / scale;
            top = (height - safeArea.yMax) / scale;
            bottom = safeArea.yMin / scale;
        }

        // The room a badge takes from its button's content: the gap before it, the badge, and
        // its distance from the button's right edge beyond the button's own side padding
        // (the badge's left and right margins in UITK). Zero without a badge. The defaults
        // are the spec's; the UITK provider passes the laid-out values, so a consumer
        // stylesheet that changes them is still honored.
        internal static float BadgeReserve(float badgeWidth, float gap = ButtonBadgeGap,
            float rightMargin = BadgeRightOffset - ButtonSidePadding)
            => badgeWidth > 0f ? gap + badgeWidth + rightMargin : 0f;

        // Where a button's content (the label, or "Continue with" and the logo) starts, from
        // the button's left edge, given its width and the badge's reserve. The rule:
        // - room for the content centered with the badge's reserve mirrored on its left: the
        //   content is centered in the button, as in the mocks;
        // - less room than that: the content moves left, as far as it has to and no further,
        //   and stays clear of the badge;
        // - not even room for the content beside the badge: it starts at the side padding and
        //   shrinks (the label ellipsizes), down to what BadgeRoom leaves it.
        // The badge never moves: BadgeRightOffset from the right edge. Without a badge this is
        // plain centering. The UITK provider sets its badged rows' content offset from this,
        // with the button's laid-out padding; the defaults are the spec's.
        internal static float ButtonContentLeft(float buttonWidth, float contentWidth, float badgeReserve,
            float paddingLeft = ButtonSidePadding, float paddingRight = ButtonSidePadding)
        {
            // As far right as the badge allows; centered if that is far enough; never left of
            // the padding (where, with less room still, the content shrinks).
            var latest = buttonWidth - paddingRight - badgeReserve - contentWidth;
            var centered = (buttonWidth - contentWidth) / 2f;
            return Mathf.Max(paddingLeft, Mathf.Min(centered, latest));
        }

        // Room kept beyond a text's measured width before it is assumed to fit: UI Toolkit can
        // lay a text out wider than MeasureTextSize reports (by up to a pt on 2022.3).
        internal const float TextLayoutSlack = 1f;

        // Whether "Continue with" fits before a logo. It is shown whole or not at all: a lead
        // text ellipsized to "Con…" reads as a glitch, while a logo alone (the native store's
        // lockup, or a PSP's wordmark) still names the choice. requiredWidth is the button
        // width the row asks for with the lead text shown (the button's side padding, the
        // lead text, the logo and its gap, the badge reserve).
        //
        // buttonWidth is the button's current width. Bottom-anchored, that is all there is.
        // Centered, the POP grows with its content (PopWidth), so a row asking for more would
        // get a wider button, up to the cap and the screen; the decision is made against that
        // width rather than the current one, or hiding the text would shrink the POP and keep
        // it hidden on a screen with room for it.
        internal static bool FitsLeadText(bool centered, float availableWidth, float buttonWidth, float requiredWidth)
        {
            // Any shortfall would ellipsize the text, hence the slack.
            var needed = requiredWidth + TextLayoutSlack;
            return needed <= ButtonWidthFor(centered, availableWidth, buttonWidth, needed);
        }

        // The width a button gets if its row asks for requiredWidth: its current width
        // bottom-anchored; centered, the width the POP would grow to for that row (up to the
        // cap and the screen), or its current width if that is already more. Decisions that
        // change what a row asks for (FitsLeadText, a lone logo's width) are made against this
        // rather than the current width, or they would keep a centered POP from growing.
        internal static float ButtonWidthFor(bool centered, float availableWidth, float buttonWidth, float requiredWidth)
        {
            if (!centered)
                return buttonWidth;
            return Mathf.Max(buttonWidth, PopWidth(true, availableWidth, requiredWidth + 2f * Padding) - 2f * Padding);
        }

        // The widest a badge is drawn in a button buttonWidth wide: what is left once the button's
        // insets (padding and border), the content's width (contentMin: all of it, the label
        // whole or "Continue with" and the logo as FitButton decided them) and the badge's gap
        // and offset (its margins) are taken; never below badgeMin (BadgeLeast), where the
        // content shrinks instead.
        // A badge as wide as this or narrower shows whole; a wider one is ellipsized to it.
        internal static float BadgeRoom(float buttonWidth, float insets, float contentMin,
            float gap, float rightMargin, float badgeMin)
            => Mathf.Max(badgeMin, buttonWidth - insets - contentMin - gap - rightMargin);

        // The button width that centers content contentWidth wide beside a badge: the badge's
        // reserve mirrored on the content's left (ButtonContentLeft's centered case), and the
        // button's padding on both sides.
        internal static float CenteredBadgedButtonWidth(float contentWidth, float badgeReserve,
            float paddingLeft = ButtonSidePadding, float paddingRight = ButtonSidePadding)
            => contentWidth + 2f * Mathf.Max(paddingLeft, paddingRight + badgeReserve);

        // Centered layout: the width a badged button asks for so that its content stays
        // centered (CenteredBadgedButtonWidth), when the POP can grow to it within its cap and
        // the screen; 0 when it can't, and then the row asks only for its content and one
        // reserve and the content moves left of center as far as the badge needs
        // (ButtonContentLeft). Its inputs are the content's and the badge's own widths and the
        // laid-out padding, never the button's current width, so asking for it can't feed back
        // into it. Bottom-anchored POPs don't grow, so this doesn't apply to them.
        internal static float CenteredBadgedButtonMinWidth(float availableWidth, float contentWidth,
            float badgeReserve, float paddingLeft = ButtonSidePadding, float paddingRight = ButtonSidePadding)
        {
            if (badgeReserve <= 0f)
                return 0f;
            var symmetric = CenteredBadgedButtonWidth(contentWidth, badgeReserve, paddingLeft, paddingRight);
            var pop = symmetric + 2f * Padding;
            return PopWidth(true, availableWidth, pop) >= pop - 0.01f ? symmetric : 0f;
        }

        // A brand logo's size on its button and its distance from "Continue with", measured
        // off the mocks (not in the spec). The Apple lockup is 17pt tall: its 141pt width in
        // dark-phone-3-buttons, which makes its "In-App Purchase" lettering as tall as the
        // capitals of the mock's 17pt label. The Google Play wordmark is 20pt (its triangle
        // 15.5pt in native-buttons-variants), and so are the Stripe and Coda logos.
        internal static float ButtonLogoHeight(PurchaseOptionViewModel.ButtonKind kind)
            => kind == PurchaseOptionViewModel.ButtonKind.Apple ? 17f : 20f;

        // The Apple lockup's baseline, as a fraction of its height from the top: in both
        // AppleInAppPurchase1ln assets (370 x 44 px) the "In-App Purchase" lettering stands on
        // row 36 (its "I" spans rows 3 to 35). Both providers set "Continue with" on it.
        internal const float AppleLockupBaseline = 36f / 44f;

        // About 7pt from "with" to the logo in the mocks (Apple 6.9, Stripe 7.7). Google Play
        // sits 13pt away in native-buttons-variants; its wordmark asset already starts 3pt in.
        internal static float ButtonLogoGap(PurchaseOptionViewModel.ButtonKind kind)
            => kind == PurchaseOptionViewModel.ButtonKind.GooglePlay ? 10f : 7f;
    }
}
