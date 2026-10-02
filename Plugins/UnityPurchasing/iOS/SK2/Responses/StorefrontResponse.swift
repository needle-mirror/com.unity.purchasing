import Foundation
import StoreKit

// The macOS bundle is compiled once and frozen into the package, so a toolchain that cannot see
// Storefront.currency would leave macOS players without a currency for the whole release with
// nothing to show it had happened. Fail the build rather than ship that.
#if os(macOS) && !compiler(>=6.1)
#error("Building for macOS needs Swift 6.1 or newer, which declares Storefront.currency.")
#endif

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public struct StorefrontResponse: ResponseProtocol, Encodable {
    let id: String
    let countryCode: String
    // ISO 4217 currency of the storefront — the same object as countryCode, so the pair is
    // coherent by construction. Storefront.currency needs iOS 17 / macOS 14 / tvOS 17;
    // before that it is nil and omitted from the JSON.
    let currencyCode: String?

    @available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
    public init(storefront: Storefront) {
        self.id = storefront.id
        self.countryCode = storefront.countryCode
        // The #available check below is a runtime one, so the symbol must still exist in the SDK
        // being compiled against, and the two versions are far apart. Storefront.currency runs
        // back to iOS 17 because it is back deployed, but it is only declared in the iOS 18.4
        // SDK, which shipped with Swift 6.1. Every earlier toolchain compiles the #available
        // check and then fails to find the property.
#if compiler(>=6.1)
        if #available(iOS 17.0, macOS 14.0, tvOS 17.0, visionOS 1.0, *) {
            self.currencyCode = storefront.currency?.identifier
        } else {
            self.currencyCode = nil
        }
#else
        self.currencyCode = nil
#endif
    }
}
