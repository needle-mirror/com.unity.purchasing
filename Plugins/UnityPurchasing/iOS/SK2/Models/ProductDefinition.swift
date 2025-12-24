import Foundation

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public struct ProductDefinition: Decodable {
    let id: String
    let storeSpecificId: String
    let type: String
    let enabled: Bool
    let payouts: [String]?
}
