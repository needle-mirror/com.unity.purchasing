import Foundation
import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public struct PurchaseDetails: Codable {
    var error: Bool?
    var expirationDate: Double?
    var originalTransactionId: UInt64?
    var ownershipType: String?
    var productId: String?
    var purchaseDate: Double?
    var signatureJws: String?
    var transactionId: UInt64?
    var verificationError: String?
    var verified: Bool?
    var appAccountToken: UUID?
    var reason: Int?

    enum CodingKeys: String, CodingKey {
        case error
        case expirationDate
        case originalTransactionId
        case ownershipType
        case productId
        case purchaseDate
        case signatureJws
        case transactionId
        case verificationError
        case verified
        case appAccountToken
        case reason
    }

    init(verificationResult: VerificationResult<Transaction>) {
        var verificationError: VerificationResult<Transaction>.VerificationError?
        var nativeTransaction: Transaction?

        switch verificationResult {
        case let .unverified(unverifiedTransaction, error):
            verificationError = error
            verified = false
            nativeTransaction = unverifiedTransaction
            break

        case let .verified(verifiedTransaction):
            verified = true
            nativeTransaction = verifiedTransaction
            break
        }

        productId = nativeTransaction!.productID
        originalTransactionId = nativeTransaction!.originalID
        ownershipType = nativeTransaction!.ownershipType.rawValue
        transactionId = nativeTransaction!.id
        expirationDate = nativeTransaction!.expirationDate?.timeIntervalSince1970
        purchaseDate = nativeTransaction!.purchaseDate.timeIntervalSince1970
        signatureJws = verificationResult.jwsRepresentation

        if verified == false {
            self.verificationError = String(describing: verificationError)
        }
        if let appAccountToken = nativeTransaction?.appAccountToken {
            self.appAccountToken = appAccountToken
        }
    }

    init(productId: String, verificationError: String, reason: Int) {
        self.productId = productId
        self.verificationError = verificationError
        self.reason = reason
        error = true
    }
}
