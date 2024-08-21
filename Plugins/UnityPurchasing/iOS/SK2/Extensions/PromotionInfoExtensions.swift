import Foundation
import StoreKit

@available(iOS 16.4, *)
@available(macOS, unavailable)
@available(tvOS, unavailable)
@available(watchOS, unavailable)
@available(visionOS, unavailable)
extension Product.PromotionInfo : Encodable {

    enum CodingKeys: String, CodingKey {
            case productId
            case visibility
        }

    public func encode(to encoder: any Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(self.productID, forKey: .productId)
        try container.encode(self.visibility.rawValue, forKey: .visibility)
    }
}
