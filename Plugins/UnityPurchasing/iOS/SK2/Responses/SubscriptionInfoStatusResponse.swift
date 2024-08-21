import Foundation
import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public class SubscriptionInfoStatusResponse: ResponseProtocol, Encodable {
    let productId: String
    var subscriptionInfoStatuses: [SubscriptionInfoStatus]?
    let statuses: [Product.SubscriptionInfo.Status]
    var responseError: SKResponseError?
    let error: Error?

    public init(productId: String, statuses: [Product.SubscriptionInfo.Status]) {
        self.productId = productId
        self.statuses = statuses
        error = nil

        subscriptionInfoStatuses = []
        addStatuses()
    }

    public init(productId: String, error: Error) {
        self.productId = productId
        self.error = error
        statuses = []

        responseError = describeError(self.error!)
    }

    enum CodingKeys: String, CodingKey {
        case productId
        case subscriptionInfoStatuses = "statuses"
        case responseError = "error"
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
extension SubscriptionInfoStatusResponse: CustomStringConvertible {
    public var description: String {
        let encoder = JSONEncoder()
        encoder.outputFormatting = .withoutEscapingSlashes

        return try! String(data: encoder.encode(self), encoding: .utf8)!
    }

    private func addStatuses() {
        if statuses.count == 0 {
            return
        }

        statuses.forEach { status in
            subscriptionInfoStatuses!.append(describeStatus(status))
        }
    }

    func describeStatus(_ status: Product.SubscriptionInfo.Status) -> SubscriptionInfoStatus {
        SubscriptionInfoStatus(
            state: status.state.value,
            transaction: status.transaction.purchaseDetails(),
            renewalInfo: status.renewalInfo.renewalInfoDetails())
    }

    func describeError(_ error: Error) -> SKResponseError {
        let kind: SKResponseError.SKErrorType
        if let _ = error as? StoreKitError {
            kind = .storeKitError
        } else {
            kind = .unknown
        }

        return SKResponseError(type: kind, description: error.localizedDescription, value: String(describing: error))
    }
}
