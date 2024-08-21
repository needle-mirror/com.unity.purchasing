import Foundation

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
struct SubscriptionInfoStatus: Codable {
    let state: String
    let transaction: PurchaseDetails
    let renewalInfo: RenewalInfoDetail
}
