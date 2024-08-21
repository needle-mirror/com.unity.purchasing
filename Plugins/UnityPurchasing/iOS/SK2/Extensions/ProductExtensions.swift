import Foundation
import StoreKit

@available(iOS 15.0, *)
extension Product : Encodable {

    enum CodingKeys: String, CodingKey {
            case id
            case displayName
            case description
            case displayPrice
            case isFamilyShareable
            case price
            case type
            case currencyCode
        }

    public func encode(to encoder: any Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(self.id, forKey: .id)
        try container.encode(self.displayName, forKey: .displayName)
        try container.encode(self.description, forKey: .description)
        try container.encode(self.displayPrice, forKey: .displayPrice)
        try container.encode(self.isFamilyShareable.description, forKey: .isFamilyShareable)
        try container.encode(self.price, forKey: .price)
        try container.encode(self.type.rawValue, forKey: .type)
        try container.encode(self.priceFormatStyle.currencyCode, forKey: .currencyCode)
    }

}
