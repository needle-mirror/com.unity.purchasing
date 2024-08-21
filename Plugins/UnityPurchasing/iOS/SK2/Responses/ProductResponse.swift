import Foundation
import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public class ProductResponse: ResponseProtocol, Encodable {
    let products: [Product]
    var productDetails: [String: ProductDetail] = [:]
    let error: Error?
    var responseError: SKResponseError?

    public init(products: [Product]) {
        self.products = products
        error = nil

        products.forEach { product in
            let (key, value) = createProduct(product)
            productDetails[key] = value
        }
    }

    public init(error: Error) {
        products = []
        self.error = error

        if let error = self.error {
            responseError = createResponseError(error)
        }
    }

    enum CodingKeys: String, CodingKey {
        case productDetails = "products"
        case responseError = "error"
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
extension ProductResponse: CustomStringConvertible {
    public var description: String {
        let encoder = JSONEncoder()
        encoder.outputFormatting = .withoutEscapingSlashes
        return try! String(data: encoder.encode(self), encoding: .utf8)!
    }

    func createProduct(_ product: Product) -> (String, ProductDetail) {
        return (product.id, ProductDetail(product: product))
    }

    func createResponseError(_ error: Error) -> SKResponseError {
        return SKResponseError(type: .storeKitError, description: error.localizedDescription, value: String(describing: error))
    }
}
