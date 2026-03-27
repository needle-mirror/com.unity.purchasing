import Foundation
import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
struct SubscriptionOfferDetails: Codable {
    var paymentMode: String?
    var price: Decimal?
    var displayPrice: String?
    var periodUnit: String?
    var periodValue: Int?
    var periodCount: Int?
    var type: String?
    var id: String?

    enum CodingKeys: String, CodingKey {
        case paymentMode
        case price
        case displayPrice
        case periodUnit = "period.unit"
        case periodValue = "period.value"
        case periodCount
        case type
        case id
    }

    init(_ subscriptionOffer: Product.SubscriptionOffer?) {
        guard let subscriptionOffer = subscriptionOffer else {
            return
        }

        paymentMode = subscriptionOffer.paymentMode.rawValue
        price = subscriptionOffer.price
        displayPrice = subscriptionOffer.displayPrice
        // To match the GetProductDetails on SK1, we don't return the rawValue
        //subscriptionPeriodUnit = subscriptionInfo.subscriptionPeriod.unit.rawValue
        switch subscriptionOffer.period.unit {
        case .day:   periodUnit = "0"
        case .week:  periodUnit = "1"
        case .month: periodUnit = "2"
        case .year:  periodUnit = "3"
        @unknown default:
            periodUnit = "0"
        }
        periodValue = subscriptionOffer.period.value
        periodCount = subscriptionOffer.periodCount
        type = subscriptionOffer.type.rawValue
        id = subscriptionOffer.id
    }
}
