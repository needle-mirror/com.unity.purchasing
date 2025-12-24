import Foundation

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public struct ProductCatalog: Decodable {
    let productIds: [String]

    enum CodingKeys: String, CodingKey {
        case productIds = "products"
    }

    public init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        productIds = try container.decode([String].self, forKey: .productIds)
    }
}
