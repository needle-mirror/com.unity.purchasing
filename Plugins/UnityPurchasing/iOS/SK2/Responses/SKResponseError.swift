import Foundation

public struct SKResponseError: Error, Codable {
    let type: SKErrorType
    let description: String
    let value: String

    enum SKErrorType: Codable {
        case productNotFound
        case customError
        case storeKitError
        case invalidJson
        case unknown
        case purchaseError
    }

    enum CodingKeys: String, CodingKey {
        case type
        case description
        case value
    }

    public func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(type, forKey: .type)
        try container.encode(description, forKey: .type)
        try container.encode(value, forKey: .type)
    }
}
