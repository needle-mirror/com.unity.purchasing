import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public class TransactionResponse: ResponseProtocol, Codable {
    private(set) var successes: [VerificationResult<Transaction>] = []
    private(set) var failures: [String] = []
    private(set) var purchases: [String: PurchaseDetails] = [:]

    public init(successes: [VerificationResult<Transaction>], failures: [String]) {
        self.successes = successes
        self.failures = failures

        successes.forEach { verificationResult in
            let details = verificationResult.purchaseDetails()
            purchases[details.productId!] = details
        }
    }

    enum CodingKeys: String, CodingKey {
        case purchases
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
extension TransactionResponse: CustomStringConvertible {
    public var description: String {
        let encoder = JSONEncoder()
        encoder.outputFormatting = .withoutEscapingSlashes

        let result = try! encoder.encode(self)

        return String(data: result, encoding: .utf8)!
    }
}
