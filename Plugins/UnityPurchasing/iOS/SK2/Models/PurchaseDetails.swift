import Foundation
import StoreKit
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public struct PurchaseDetails: Codable {
    var error: Bool?
    var expirationDate: Double?
    var revocationDate: Double?
    var offerId: String?
    var offerType: Int?
    var originalTransactionId: UInt64?
    var ownershipType: String?
    var productId: String?
    var productType: String?
    var purchaseDate: Double?
    var signatureJws: String?
    var transactionId: UInt64?
    var verificationError: String?
    var verified: Bool?
    var appAccountToken: UUID?
    var reason: Int?
    var isFree: Bool?

    var autoRenewPreference: String?

    var productJsonRepresentation: String?
    var transactionJsonRepresentation: String?

    enum CodingKeys: String, CodingKey {
        case error
        case expirationDate
        case revocationDate
        case offerId
        case offerType
        case originalTransactionId
        case ownershipType
        case productId
        case productType
        case purchaseDate
        case signatureJws
        case transactionId
        case verificationError
        case verified
        case appAccountToken
        case reason
        case isFree
        case autoRenewPreference
        case productJsonRepresentation
        case transactionJsonRepresentation
    }

    init(verificationResult: VerificationResult<Transaction>) {
        var verificationError: VerificationResult<Transaction>.VerificationError?
        var nativeTransaction: Transaction?
        var decodedPayload: JWSTransactionDecodedPayload?

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
        productType = nativeTransaction!.productType.rawValue

        originalTransactionId = nativeTransaction!.originalID

        ownershipType = nativeTransaction!.ownershipType.rawValue

        transactionId = nativeTransaction!.id

        expirationDate = nativeTransaction!.expirationDate?.timeIntervalSince1970
        revocationDate = nativeTransaction!.revocationDate?.timeIntervalSince1970
        purchaseDate = nativeTransaction!.purchaseDate.timeIntervalSince1970

        signatureJws = verificationResult.jwsRepresentation

        offerType = nativeTransaction!.offerType?.rawValue
        offerId = nativeTransaction!.offerID

        // Avoid decoding JSON transaction for products which do not have `subscriptionInfo`
        // Using this workaround to get price from transactions, as otherwise we would need rely on functions only available in versions of xcode 15.1+
        // If these fields become useful for non-subscription products, will need to rework.
        if (nativeTransaction!.productType == Product.ProductType.autoRenewable || nativeTransaction!.productType == Product.ProductType.nonRenewable) {
            let rawPayload = nativeTransaction!.jsonRepresentation
            decodedPayload = try? JSONDecoder().decode(JWSTransactionDecodedPayload.self, from: rawPayload)
        }

        // TODO: return price (decodedPayload.price / 1000) when refactoring product and order info
        isFree = decodedPayload?.price.map {$0 == 0}

        if verified == false {
            self.verificationError = String(describing: verificationError)
        }
        if let appAccountToken = nativeTransaction?.appAccountToken {
            self.appAccountToken = appAccountToken
        }

        if let transaction = nativeTransaction {
            generateTransactionJsonRepresentation(from: transaction)
        }
    }

    init(verificationResult: VerificationResult<Transaction>, nativeProduct: Product) async {
        self.init(verificationResult: verificationResult)

        // Json representation for Attribution SDK
        generateProductJsonRepresentation(from: nativeProduct)

        if let subscription = nativeProduct.subscription {
            do {
                let statuses = try await subscription.status
                if let status = statuses.first {
                    switch status.renewalInfo {
                    case .verified(let info):
                        self.autoRenewPreference = info.autoRenewPreference
                    case .unverified(_, _):
                        break // Ignore unverified renewal info
                    }
                }
            } catch {
                // Ignore errors fetching subscription status
            }
        }
    }

    init(productId: String, verificationError: String, reason: Int) {
        self.productId = productId
        self.verificationError = verificationError
        self.reason = reason
        error = true
    }

    // Helper method to generate transaction JSON representation
    private mutating func generateTransactionJsonRepresentation(from transaction: Transaction) {
        self.transactionJsonRepresentation = transaction.jsonRepresentation.base64EncodedString()
    }

    // Helper method to generate product JSON representation
    private mutating func generateProductJsonRepresentation(from product: Product) {
        self.productJsonRepresentation = product.jsonRepresentation.base64EncodedString()
    }
}

