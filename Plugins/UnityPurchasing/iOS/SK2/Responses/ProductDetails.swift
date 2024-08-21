import Foundation
import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
struct ProductDetail: Codable {
    let productId: String
    let displayName: String
    let description: String
    let displayPrice: String
    let price: Decimal
    let type: String
    let isFamilyShareable: Bool
    let subscriptionInfo: SubscriptionInfoDetails?


    init(product: Product) {
        productId = product.id
        displayName = product.displayName
        description = product.description
        displayPrice = product.displayPrice
        price = product.price
        type = product.type.rawValue
        isFamilyShareable = product.isFamilyShareable

        guard let subscriptionInfo = product.subscription else {
            self.subscriptionInfo = nil
            return
        }

        self.subscriptionInfo = SubscriptionInfoDetails(subscriptionInfo)
    }
}
