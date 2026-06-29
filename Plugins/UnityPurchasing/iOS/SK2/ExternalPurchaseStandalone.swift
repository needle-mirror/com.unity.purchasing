import Foundation
import StoreKit

/**
 Standalone implementations of Apple's ExternalPurchaseCustomLink API.

 Extracted from `StoreKitManager` so the standalone path never touches the
 DI-dependent singleton. Methods here are static and stateless — they only
 invoke the per-call callback pointer, never the `storeKitCallback` registered
 in the DependencyContainer. Safe to call before any IAP service initialization,
 and safe to mix with later `UnityIAPServices` initialization without poisoning
 `StoreKitManager.instance` with stale dependencies.

 See `ExternalPurchaseClient.cs` for the C# API.
 See: https://developer.apple.com/support/communication-and-promotion-of-offers-on-the-app-store-in-the-eu/
 */
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public enum ExternalPurchaseStandalone {

    // EU member state country codes (ISO 3166-1 alpha-3) for manual eligibility fallback on pre-iOS 18.1.
    // Source: https://developer.apple.com/support/communication-and-promotion-of-offers-on-the-app-store-in-the-eu/
    private static let euCountryCodes: Set<String> = [
        "AUT", "BEL", "BGR", "HRV", "CYP", "CZE", "DNK", "EST", "FIN", "FRA",
        "DEU", "GRC", "HUN", "IRL", "ITA", "LVA", "LTU", "LUX", "MLT", "NLD",
        "POL", "PRT", "ROU", "SVK", "SVN", "ESP", "SWE"
    ]

    // Accepted token-type strings (matches the C# `ExternalPurchaseTokenType` enum + future Japan IN_APP).
    private static let validTokenTypes: Set<String> = ["ACQUISITION", "SERVICES", "LINK_OUT", "IN_APP"]

    // Token types that require Japan MSCA platform versions (gated by iOS 26.4 / macOS 26.4 / visionOS 26.4).
    private static let japanTokenTypes: Set<String> = ["LINK_OUT", "IN_APP"]

    /// Helper to invoke an ExternalPurchaseCallbackDelegateType on the main thread with heap-allocated strings.
    private static func invokeCallback(
        _ callback: @escaping ExternalPurchaseCallbackDelegateType,
        subject: String,
        payload: String
    ) {
        let subjectCopy = unityPurchasingMakeHeapAllocatedStringCopy(subject)
        let payloadCopy = unityPurchasingMakeHeapAllocatedStringCopy(payload)
        Task { @MainActor in
            callback(subjectCopy, payloadCopy)
        }
    }

    /// Manual eligibility fallback for pre-iOS 18.1: canMakePayments + EU storefront country code.
    /// Japan/Korea/USA don't need fallback — their support was added in iOS 18.1+.
    private static func manualEligibilityCheck() async -> Bool {
        guard AppStore.canMakePayments else { return false }
        guard let storefront = await Storefront.current else { return false }
        return euCountryCodes.contains(storefront.countryCode)
    }

    /**
     Check if external purchase is eligible for this user.
     iOS 18.1+: uses ExternalPurchaseCustomLink.isEligible (entitlement-based).
     iOS 15.0–18.0: falls back to canMakePayments + storefront country code check (EU only).
     Standalone — invokes the callback pointer directly (not through storeKitCallback).
     */
    public static func checkEligibility(callback: @escaping ExternalPurchaseCallbackDelegateType) async {
#if compiler(>=6.0)
        let isEligible: Bool
        if #available(iOS 18.1, macOS 15.1, visionOS 2.1, *) {
            isEligible = await ExternalPurchaseCustomLink.isEligible
        } else {
            // Pre-18.1 fallback: canMakePayments + EU storefront check
            isEligible = await manualEligibilityCheck()
        }
        let response = ExternalPurchaseEligibilityResponse(isEligible: isEligible)
        let jsonString = encodeToJSON(response)
        invokeCallback(callback, subject: "OnCheckExternalPurchaseEligibilitySucceeded", payload: jsonString)
#else
        invokeCallback(callback, subject: "OnCheckExternalPurchaseEligibilityFailed", payload: "ExternalPurchaseCustomLink requires Xcode 16+ and iOS 18.1+ / macOS 15.1+ / visionOS 2.1+")
#endif
    }

    /**
     Fetch an external purchase token to associate with customer account.
     Standalone — invokes the callback pointer directly.
     - Parameter tokenType: "ACQUISITION", "SERVICES", "LINK_OUT", or "IN_APP"
       (IN_APP / LINK_OUT also require iOS 26.4+ for Japan MSCA compliance.)
     */
    public static func fetchToken(tokenType: String, callback: @escaping ExternalPurchaseCallbackDelegateType) async {
#if compiler(>=6.0)
        guard #available(iOS 18.1, macOS 15.1, visionOS 2.1, *) else {
            invokeCallback(callback, subject: "OnFetchExternalPurchaseTokenFailed", payload: "ExternalPurchaseCustomLink requires iOS 18.1+ / macOS 15.1+ / visionOS 2.1+")
            return
        }

        // Validate token type
        let normalizedType = tokenType.uppercased()
        guard validTokenTypes.contains(normalizedType) else {
            invokeCallback(callback, subject: "OnFetchExternalPurchaseTokenFailed", payload: "Invalid token type: \(tokenType). Use ACQUISITION, SERVICES, LINK_OUT, or IN_APP.")
            return
        }

        // LINK_OUT and IN_APP tokens require iOS 26.4 / macOS 26.4 / visionOS 26.4
        // (Japan MSCA). Apple's post-WWDC25 unified version numbering makes these
        // versions coextensive. Without the macOS/visionOS entries, the * wildcard
        // would let those platforms bypass the guard at any version and hit the
        // underlying API on releases that don't yet expose Japan token types.
        // If we need to support 26.2/26.3 in the future, this guard and the C# layer
        // (ApplePaymentProviderImpl) must be adjusted.
        if japanTokenTypes.contains(normalizedType) {
            guard #available(iOS 26.4, macOS 26.4, visionOS 26.4, *) else {
                invokeCallback(callback, subject: "OnFetchExternalPurchaseTokenFailed", payload: "Token type \(tokenType) requires iOS 26.4+ / macOS 26.4+ / visionOS 26.4+ (Japan MSCA compliance).")
                return
            }
        }

        do {
            guard let token = try await ExternalPurchaseCustomLink.token(for: normalizedType) else {
                invokeCallback(callback, subject: "OnFetchExternalPurchaseTokenFailed", payload: "No token returned")
                return
            }
            let response = ExternalPurchaseTokenResponse(tokenValue: token.value ?? "", tokenType: tokenType)
            let jsonString = encodeToJSON(response)
            invokeCallback(callback, subject: "OnFetchExternalPurchaseTokenSucceeded", payload: jsonString)
        } catch {
            invokeCallback(callback, subject: "OnFetchExternalPurchaseTokenFailed", payload: error.localizedDescription)
        }
#else
        invokeCallback(callback, subject: "OnFetchExternalPurchaseTokenFailed", payload: "ExternalPurchaseCustomLink requires Xcode 16+ and iOS 18.1+ / macOS 15.1+ / visionOS 2.1+")
#endif
    }

    /**
     Show Apple's required notice before linking to external purchase.
     Standalone — invokes the callback pointer directly.
     - Parameter noticeType: "BROWSER" (opens in Safari) or "WITHINAPP" (opens in web view)

     Annotated `@MainActor` because `ExternalPurchaseCustomLink.showNotice` presents a
     system UI sheet; UIKit/SwiftUI presentation APIs must be invoked on the main thread.
     The caller (`externalPurchase_ShowNotice` cdecl) spawns this from a detached
     background task — without `@MainActor` here, the system-UI presentation would
     potentially trip Main Thread Checker even when Apple's API handles its own hop.
     */
    @MainActor
    public static func showNotice(noticeType: String, callback: @escaping ExternalPurchaseCallbackDelegateType) async {
#if compiler(>=6.0)
        guard #available(iOS 18.1, macOS 15.1, visionOS 2.1, *) else {
            invokeCallback(callback, subject: "OnShowExternalPurchaseNoticeFailed", payload: "ExternalPurchaseCustomLink requires iOS 18.1+ / macOS 15.1+ / visionOS 2.1+")
            return
        }

        // Convert to NoticeType enum
        let skNoticeType: ExternalPurchaseCustomLink.NoticeType
        switch noticeType.uppercased() {
        case "BROWSER":
            skNoticeType = .browser
        case "WITHINAPP":
            skNoticeType = .withinApp
        default:
            invokeCallback(callback, subject: "OnShowExternalPurchaseNoticeFailed", payload: "Invalid notice type: \(noticeType). Use BROWSER or WITHINAPP.")
            return
        }

        do {
            let result = try await ExternalPurchaseCustomLink.showNotice(type: skNoticeType)
            if result == .continued {
                invokeCallback(callback, subject: "OnShowExternalPurchaseNoticeSucceeded", payload: "")
            } else {
                invokeCallback(callback, subject: "OnShowExternalPurchaseNoticeFailed", payload: "UserCancelled")
            }
        } catch {
            invokeCallback(callback, subject: "OnShowExternalPurchaseNoticeFailed", payload: error.localizedDescription)
        }
#else
        invokeCallback(callback, subject: "OnShowExternalPurchaseNoticeFailed", payload: "ExternalPurchaseCustomLink requires Xcode 16+ and iOS 18.1+ / macOS 15.1+ / visionOS 2.1+")
#endif
    }

    /**
     Fetch the current storefront country code.
     Standalone — invokes the callback pointer directly (not through storeKitCallback).
     Used by ApplePaymentProviderImpl for region-based token type selection.
     */
    public static func fetchStorefront(callback: @escaping ExternalPurchaseCallbackDelegateType) async {
        if let storefront = await Storefront.current {
            let response = StorefrontResponse(storefront: storefront)
            let jsonString = encodeToJSON(response)
            invokeCallback(callback, subject: "OnFetchStorefrontSucceeded", payload: jsonString)
        } else {
            invokeCallback(callback, subject: "OnFetchStorefrontFailed", payload: "No storefront available")
        }
    }
}
