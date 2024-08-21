import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
extension Product.SubscriptionInfo.RenewalState {
    public var value: String {
        switch self {
        case .expired:
            return "expired"
        case .inBillingRetryPeriod:
            return "inBillingRetryPeriod"
        case .inGracePeriod:
            return "inGracePeriod"
        case .revoked:
            return "revoked"
        case .subscribed:
            return "subscribed"
        default:
            return "unknown"
        }
    }
}
