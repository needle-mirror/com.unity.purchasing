import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
extension Product.SubscriptionPeriod.Unit {
    var rawValue: String {
        switch self {
        case .day:
            return "Day"
        case .month:
            return "Month"
        case .week:
            return "Week"
        case .year:
            return "Year"
        @unknown default:
            return "NotAvailable"
        }
    }
}
