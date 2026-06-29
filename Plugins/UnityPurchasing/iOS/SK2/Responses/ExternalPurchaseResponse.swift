import Foundation
import StoreKit

#if compiler(>=6.0)
// MARK: - Eligibility Response
// Always succeeds - returns whether user is eligible or not

public struct ExternalPurchaseEligibilityResponse: ResponseProtocol, Encodable {
    let isEligible: Bool

    public init(isEligible: Bool) {
        self.isEligible = isEligible
    }
}

// MARK: - Token Response
// Success case only - failure passes error string directly

@available(iOS 18.1, macOS 15.1, visionOS 2.1, *)
public struct ExternalPurchaseTokenResponse: ResponseProtocol, Encodable {
    let token: String
    let tokenType: String

    @available(iOS 18.1, macOS 15.1, visionOS 2.1, *)
    public init(tokenValue: String, tokenType: String) {
        self.token = tokenValue
        self.tokenType = tokenType
    }
}
#endif
