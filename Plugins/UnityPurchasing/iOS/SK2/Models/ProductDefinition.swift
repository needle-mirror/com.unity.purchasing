import Foundation

public struct ProductDefinition: Decodable {
    let id: String
    let storeSpecificId: String
    let type: String
    let enabled: Bool
    let payouts: [String]?
}
