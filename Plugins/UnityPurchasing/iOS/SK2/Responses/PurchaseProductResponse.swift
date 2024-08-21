import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public typealias PurchaseProductResult = Result<Product.PurchaseResult, SKResponseError>

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
struct PurchaseAttempt: Codable {
    var productId: String
    var result: PurchaseProductStatus?
    var error: SKResponseError?

    enum CodingKeys: String, CodingKey {
        case productId
        case result
        case error
    }

    public func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(productId, forKey: .productId)
        try container.encode(result, forKey: .result)
        try container.encode(error, forKey: .error)
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public class PurchaseProductResponse: ResponseProtocol, Encodable {
    var result: PurchaseProductResult
    var purchase: PurchaseAttempt?

    public init(productId: String, result: PurchaseProductResult) {
        self.result = result

        var status: PurchaseProductStatus?
        var error: SKResponseError?

        switch self.result {
        case let .success(purchaseResult):
            status = describeSuccess(result: purchaseResult)
        case let .failure(purchaseProductError):
            error = purchaseProductError
        }

        purchase = PurchaseAttempt(productId: productId, result: status, error: error)
    }

    enum CodingKeys: String, CodingKey {
        case purchase
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
extension PurchaseProductResponse: CustomStringConvertible {
    public var description: String {
        let encoder = JSONEncoder()
        encoder.outputFormatting = .withoutEscapingSlashes

        let result = try! encoder.encode(self)

        return String(data: result, encoding: .utf8)!
    }

    func describeSuccess(result: Product.PurchaseResult) -> PurchaseProductStatus {
        var purchaseDetail: PurchaseDetails?
        if case let Product.PurchaseResult.success(verificationResult) = result {
            purchaseDetail = verificationResult.purchaseDetails()
        }

        return PurchaseProductStatus(
            status: getTrimmedEnumName(describing: String(describing: result)),
            purchaseDetail: purchaseDetail)
    }

    func getTrimmedEnumName(describing: String) -> String {
        let name: String
        if let i = describing.firstIndex(where: { $0 == "(" }) {
            name = String(describing[..<i])
        } else {
            name = describing
        }
        return name
    }
}
