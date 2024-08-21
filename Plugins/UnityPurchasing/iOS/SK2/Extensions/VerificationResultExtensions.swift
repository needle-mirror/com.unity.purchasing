import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
extension VerificationResult where SignedType == Transaction {
    func purchaseDetails() -> PurchaseDetails {
        PurchaseDetails(verificationResult: self)
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
extension VerificationResult where SignedType == Product.SubscriptionInfo.RenewalInfo {
    func renewalInfoDetails() -> RenewalInfoDetail {
        RenewalInfoDetail(verificationResult: self)
    }
}
