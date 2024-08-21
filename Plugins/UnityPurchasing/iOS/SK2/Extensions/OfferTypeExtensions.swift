import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
extension Transaction.OfferType {
    public var value: String {
        switch self {
        case Transaction.OfferType.code:
            return "code"
        case Transaction.OfferType.introductory:
            return "introductory"
        case Transaction.OfferType.promotional:
            return "promotional"
        default:
            return "unknown"
        }
    }
}

