import Foundation
import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
struct RenewalInfoDetail: Codable {
    let verified: Bool
    let originalTransactionId: UInt64
    let currentProductId: String
    let willAutoRenew: Bool
    let isInBillingRetry: Bool
    let deviceVerification: String
    let deviceVerificationNonce: String
    let priceIncreaseStatus: String
    let signedDate: Double

    var autoRenewPreference: String?
    var offerId: String?
    var verificationError: String?
    var expirationReason: String?
    var gracePeriodExpirationDate: Double?
    var offerType: String?
    var offerTypeRawValue: Int?

    enum CodingKeys: String, CodingKey {
        case verified
        case originalTransactionId = "originalTransactionID"
        case currentProductId = "currentProductID"
        case willAutoRenew
        case isInBillingRetry
        case deviceVerification
        case deviceVerificationNonce
        case priceIncreaseStatus
        case signedDate

        case autoRenewPreference
        case offerId = "offerID"
        case verificationError
        case expirationReason
        case gracePeriodExpirationDate
        case offerType
        case offerTypeRawValue = "offerType.rawValue"
    }

    init(verificationResult: VerificationResult<Product.SubscriptionInfo.RenewalInfo>) {
        var verificationError: VerificationResult<Product.SubscriptionInfo.RenewalInfo>.VerificationError?
        var renewalInfo: Product.SubscriptionInfo.RenewalInfo
        var verified = false

        switch verificationResult {
        case let .unverified(unverifiedRenewalInfo, error):
            verificationError = error
            verified = false
            renewalInfo = unverifiedRenewalInfo
            break

        case let .verified(verifiedRenewalInfo):
            verified = true
            renewalInfo = verifiedRenewalInfo
            break
        }

        self.verified = verified
        originalTransactionId = renewalInfo.originalTransactionID
        currentProductId = renewalInfo.currentProductID
        willAutoRenew = renewalInfo.willAutoRenew
        isInBillingRetry = renewalInfo.isInBillingRetry
        deviceVerification = renewalInfo.deviceVerification.base64EncodedString()
        deviceVerificationNonce = String(describing: renewalInfo.deviceVerificationNonce)
        priceIncreaseStatus = renewalInfo.priceIncreaseStatus.value
        signedDate = renewalInfo.signedDate.timeIntervalSince1970

        autoRenewPreference = renewalInfo.autoRenewPreference

        offerId = renewalInfo.offerID

        if verified == false {
            self.verificationError = String(describing: verificationError)
        }

        expirationReason = renewalInfo.expirationReason?.value

        gracePeriodExpirationDate = renewalInfo.gracePeriodExpirationDate?.timeIntervalSince1970

        if let offerType = renewalInfo.offerType {
            self.offerType = offerType.value
            offerTypeRawValue = offerType.rawValue
        }
    }
}
