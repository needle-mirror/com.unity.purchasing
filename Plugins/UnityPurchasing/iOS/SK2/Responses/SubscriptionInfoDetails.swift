import Foundation
import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
struct SubscriptionInfoDetails: Codable {
    let subscriptionGroupId: String
    let subscriptionPeriodUnit: String
    let subscriptionPeriodValue: Int
    let introductoryOffer: SubscriptionOfferDetails
    let promotionalOffers: [SubscriptionOfferDetails]

    enum CodingKeys: String, CodingKey {
        case subscriptionGroupId = "subscriptionGroupID"
        case subscriptionPeriodUnit = "subscriptionPeriod.unit"
        case subscriptionPeriodValue = "subscriptionPeriod.value"
        case introductoryOffer
        case promotionalOffers
    }

    init(_ subscriptionInfo: Product.SubscriptionInfo) {
        subscriptionGroupId = subscriptionInfo.subscriptionGroupID
        subscriptionPeriodUnit = subscriptionInfo.subscriptionPeriod.unit.rawValue
        subscriptionPeriodValue = subscriptionInfo.subscriptionPeriod.value

        introductoryOffer = SubscriptionOfferDetails(subscriptionInfo.introductoryOffer)
        promotionalOffers = subscriptionInfo.promotionalOffers.map({ offer in
            SubscriptionOfferDetails(offer)
        })
    }
}
