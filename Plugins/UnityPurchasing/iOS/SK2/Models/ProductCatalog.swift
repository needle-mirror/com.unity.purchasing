import Foundation

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
