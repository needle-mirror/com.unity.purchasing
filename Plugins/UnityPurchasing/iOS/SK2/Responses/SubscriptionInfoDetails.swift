import Foundation
import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
struct SubscriptionInfoDetails: Codable {
    let subscriptionGroupId: String
    let subscriptionPeriodUnit: String
    let subscriptionPeriodValue: Int
    let isEligibleForIntroOffer: Bool
    let introductoryOffer: SubscriptionOfferDetails?
    let promotionalOffers: [SubscriptionOfferDetails]

    enum CodingKeys: String, CodingKey {
        case subscriptionGroupId = "subscriptionGroupID"
        case subscriptionPeriodUnit = "subscriptionPeriod.unit"
        case subscriptionPeriodValue = "subscriptionPeriod.value"
        case isEligibleForIntroOffer
        case introductoryOffer
        case promotionalOffers
    }

    init(_ subscriptionInfo: Product.SubscriptionInfo, isEligibleForIntroOffer: Bool) {
        subscriptionGroupId = subscriptionInfo.subscriptionGroupID
        // To match the GetProductDetails on SK1, we don't return the rawValue
        //subscriptionPeriodUnit = subscriptionInfo.subscriptionPeriod.unit.rawValue
        switch subscriptionInfo.subscriptionPeriod.unit {
        case .day:   subscriptionPeriodUnit = "0"
        case .week:  subscriptionPeriodUnit = "1"
        case .month: subscriptionPeriodUnit = "2"
        case .year:  subscriptionPeriodUnit = "3"
        @unknown default:
            subscriptionPeriodUnit = "0"
        }
        subscriptionPeriodValue = subscriptionInfo.subscriptionPeriod.value

        self.isEligibleForIntroOffer = isEligibleForIntroOffer
        if isEligibleForIntroOffer {
            introductoryOffer = subscriptionInfo.introductoryOffer.map({SubscriptionOfferDetails($0)})
        } else {
            introductoryOffer = nil
        }
        promotionalOffers = subscriptionInfo.promotionalOffers.map({ offer in
            SubscriptionOfferDetails(offer)
        })
    }
}
