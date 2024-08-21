import Foundation

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
struct PurchaseProductStatus: Codable {
    let status: String
    let purchaseDetail: PurchaseDetails?

    enum CodingKeys: String, CodingKey {
        case status
        case purchaseDetail
    }
}
