import Foundation
import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public struct StorefrontResponse: ResponseProtocol, Encodable {
    let id: String
    let countryCode: String

    @available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
    public init(storefront: Storefront) {
        self.id = storefront.id
        self.countryCode = storefront.countryCode
    }
}
