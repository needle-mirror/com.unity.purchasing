import Foundation
import StoreKit

/**
    Convert the purchase options dictionary from Unity to the purchase options for StoreKit
 */
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public class PurchaseProductOptionsConverter {
    public static func makePurchaseOptions(purchaseOptionsRequestJson: [String: AnyObject], storefrontChangeCallback: StorefrontCallbackDelegateType? = nil) -> Set<Product.PurchaseOption> {
        return extractPurchaseOptions(purchaseOptionsJson: purchaseOptionsRequestJson, storefrontChangeCallback: storefrontChangeCallback)
    }

    private static func extractPurchaseOptions(purchaseOptionsJson: [String: AnyObject], storefrontChangeCallback: StorefrontCallbackDelegateType?) -> Set<Product.PurchaseOption> {
        var result = Set<Product.PurchaseOption>()
        extractPurchaseOption(result: &result, purchaseOptionsJson: purchaseOptionsJson, optionName: "appAccountToken", optionConverter: makeAppAccountToken)
        extractPurchaseOption(result: &result, purchaseOptionsJson: purchaseOptionsJson, optionName: "promotionalOffer", optionConverter: makePromotionalOfferOption)
        extractPurchaseOption(result: &result, purchaseOptionsJson: purchaseOptionsJson, optionName: "quantity", optionConverter: makeQuantityOption)
        extractPurchaseOption(result: &result, purchaseOptionsJson: purchaseOptionsJson, optionName: "simulatesAskToBuyInSandbox", optionConverter: makeSimulatesAskToBuyInSandboxOption)

        if let callback = storefrontChangeCallback {
            let capturedMakeOnStorefrontChangeOption = { (purchaseOptionJson: [String: AnyObject], optionName: String) -> StoreKit.Product.PurchaseOption? in
                makeOnStorefrontChangeOption(purchaseOptionJson: purchaseOptionJson, storefrontChangeCallback: callback)
            }
            extractPurchaseOption(result: &result, purchaseOptionsJson: purchaseOptionsJson, optionName: "onStorefrontChange", optionConverter: capturedMakeOnStorefrontChangeOption)
        }

        return result
    }

    private static func extractPurchaseOption(result: inout Set<Product.PurchaseOption>, purchaseOptionsJson: [String: AnyObject], optionName: String, optionConverter: ([String: AnyObject], String) -> Product.PurchaseOption?) {
        if let option = optionConverter(purchaseOptionsJson, optionName) {
            result.insert(option)
        }
    }

    private static func makeAppAccountToken(purchaseOptionJson: [String: AnyObject], optionName: String) -> Product.PurchaseOption? {
        if let value = purchaseOptionJson[optionName] as? String {
            if let token = UUID(uuidString: value) {
                return Product.PurchaseOption.appAccountToken(token)
            }
        }
        return nil
    }

    private static func makePromotionalOfferOption(purchaseOptionJson: [String: AnyObject], optionName: String) -> Product.PurchaseOption? {
        var offerID: String?
        var keyID: String?
        var nonce: UUID?
        var signature: Data?
        var timestamp: Int?

        for (key, value) in purchaseOptionJson {
            if key == "offerID", let value = value as? String {
                offerID = value
            }
            if key == "keyID", let value = value as? String {
                keyID = value
            }
            if key == "nonce", let value = value as? String {
                if let value = UUID(uuidString: value) {
                    nonce = value
                }
            }
            if key == "signature", let value = value as? String {
                signature = Data(base64Encoded: value)
            }
            if key == "timestamp", let value = value as? Int {
                timestamp = value
            }
        }

        if offerID != nil, keyID != nil, nonce != nil, signature != nil, timestamp != nil {
            return Product.PurchaseOption.promotionalOffer(offerID: offerID!, keyID: keyID!, nonce: nonce!, signature: signature!, timestamp: timestamp!)
        }

        return nil
    }

    private static func makeQuantityOption(purchaseOptionJson: [String: AnyObject], optionName: String) -> Product.PurchaseOption? {
        if let value = purchaseOptionJson[optionName] as? Int {
            return Product.PurchaseOption.quantity(value)
        }
        return nil
    }

    private static func makeSimulatesAskToBuyInSandboxOption(purchaseOptionJson: [String: AnyObject], optionName: String) -> Product.PurchaseOption? {
        if let value = purchaseOptionJson[optionName] as? Bool {
            return Product.PurchaseOption.simulatesAskToBuyInSandbox(value)
        }
        return nil
    }

    private static func makeOnStorefrontChangeOption(purchaseOptionJson: [String: AnyObject], storefrontChangeCallback: @escaping StorefrontCallbackDelegateType) -> Product.PurchaseOption? {
        let callback = { (storefront: Storefront) -> Bool in
            storefrontChangeCallback(storefront.countryCode, storefront.id)
        }
        return Product.PurchaseOption.onStorefrontChange(shouldContinuePurchase: callback)
    }
}
