import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
extension Product.SubscriptionInfo.RenewalInfo.ExpirationReason {
    public var value: String {
        switch self {
        case .autoRenewDisabled:
            return "autoRenewDisabled"
        case .billingError:
            return "billingError"
        case .didNotConsentToPriceIncrease:
            return "didNotConsentToPriceIncrease"
        case .productUnavailable:
            return "productUnavailable"
        case .unknown:
            fallthrough
        default:
            return "unknown"
        }
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
extension Product.SubscriptionInfo.RenewalInfo.PriceIncreaseStatus {
    public var value: String {
        switch self {
        case .agreed:
            return "agreed"
        case .noIncreasePending:
            return "noIncreasePending"
        case .pending:
            return "pending"
        }
    }
}
