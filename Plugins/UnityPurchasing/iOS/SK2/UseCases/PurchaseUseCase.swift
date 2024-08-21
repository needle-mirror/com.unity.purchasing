import Foundation
import StoreKit

/**
 This protocol can be implemented by any classes that can be used to perform StoreKit product purchase operations.
 */
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public protocol PurchaseUseCaseProtocol {
    func purchaseProduct(productId: String, options: [String: AnyObject], storefrontChangeCallback: StorefrontCallbackDelegateType?) async -> PurchaseDetails?
    func addPurchaseIntentListener()
    func activateInterceptPromotionalPurchases()
    func continuePromotionalPurchases() async
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public class PurchaseUseCase: NSObject, PurchaseUseCaseProtocol {
    @Dependency private(set) var fetchProductsUseCase: ProductUseCaseProtocol
    @Dependency private(set) var transactionObserver: TransactionObserverUseCaseProtocol
    @Dependency private(set) var storeKitCallback: StoreKitCallbackDelegate

    var purchaseIntentTask: Task<Void, Error>? = nil
    var interceptPromotionalPurchases = false;
    var interceptedProductIds: [String] = []

    override init() {
        super.init()
    }

    deinit {
        // Remove the class as a transaction observer
        SKPaymentQueue.default().remove(self)
        purchaseIntentTask?.cancel()
    }


    public func purchaseProduct(productId: String, options: [String: AnyObject], storefrontChangeCallback: StorefrontCallbackDelegateType?) async -> PurchaseDetails? {
        guard let product = await fetchProductsUseCase.fetchProduct(for: productId) else {
            return nil
        }

        return await purchaseProduct(product: product, options: options, storefrontChangeCallback: storefrontChangeCallback)
    }

    private func purchaseProduct(product: Product, options: [String: AnyObject], storefrontChangeCallback: StorefrontCallbackDelegateType?) async -> PurchaseDetails? {
        let purchaseProductOptions = PurchaseProductOptionsConverter.makePurchaseOptions(purchaseOptionsRequestJson: options, storefrontChangeCallback: storefrontChangeCallback)
        do {
#if os(visionOS)
            let scene = await (UIApplication.shared.connectedScenes.first as? UIWindowScene)?.windows.first
            let purchaseResult = try await product.purchase(confirmIn: (scene?.windowScene)!, options: Set(purchaseProductOptions))
            switch purchaseResult {
            case .success(let verification):
                _ = try transactionObserver.checkVerified(verification)
                return verification.purchaseDetails()
            case .userCancelled:
                return nil
            case .pending:
                let jsonString = encodeToJSON( ["products": [product]])
                await storeKitCallback.callback(subject: "OnPurchaseDeferred", payload: jsonString, entitlementStatus: 0)
                return nil
            default:
                return nil
            }
#else
            // This is a signed & verified transaction. StoreKit handle transaction verification for us.
            guard let purchaseDetail = try await purchase(product, options: purchaseProductOptions) else {
                return nil
            }
            return purchaseDetail
#endif
        }
        catch {
            printLog("Failed purchasing a product from the App Store server. \(error)")
            return nil
        }
    }

    /**
     Purchase the `Product`. Once the StoreKit returns result of the purchase, the function checks whether the transaction is verified.
     If it is verified, this function returns the content to the user using transactionObserver and finish a transaction.
     If it isn't verified, this function rethrows the verification error.
     */
#if !os(visionOS)
    private func purchase(_ product: Product, options: Set<Product.PurchaseOption>) async throws -> PurchaseDetails? {
        let result = try await product.purchase(options: options)
        switch result {
        case .success(let verification):
            _ = try transactionObserver.checkVerified(verification)
            return verification.purchaseDetails()
        case .userCancelled:
            return nil
        case .pending:
            let jsonString = encodeToJSON( ["products": [product]])
            await storeKitCallback.callback(subject: "OnPurchaseDeferred", payload: jsonString, entitlementStatus: 0)
            return nil
        default:
            return nil
        }
    }
#endif

    public func addPurchaseIntentListener() {
#if os(iOS) || (os(macOS) && compiler(>=5.10))
        if #available(iOS 16.4, macOS 14.4, *) {
            purchaseIntentTask = listenPurchaseIntent()
        } else {
            SKPaymentQueue.default().add(self)
        }
#endif
    }

#if os(iOS) || (os(macOS) && compiler(>=5.10))
    @available(iOS 16.4, macOS 14.4, *)
    @available(tvOS, unavailable)
    @available(watchOS, unavailable)
    @available(visionOS, unavailable)
    private func listenPurchaseIntent() -> Task<Void, Error> {
            return Task.detached {
                let options:[String: AnyObject] = [:]
                for await purchaseIntent in PurchaseIntent.intents {
                    if (self.interceptPromotionalPurchases)
                    {
                        Task.detached(priority: .background, operation: {
                            await self.storeKitCallback.callbackPtr(subject: "OnPromotionalPurchaseAttempted", payload: purchaseIntent.id, entitlementStatus: 0)
                        })

                        self.interceptedProductIds.append(purchaseIntent.id)
                    } else {
                        // Receive the purchase intent and then complete the purchase workflow.
                        _ = await self.purchaseProduct(product: purchaseIntent.product, options: options, storefrontChangeCallback: nil)
                    }
                }
            }
        }
#endif

    public func activateInterceptPromotionalPurchases() {
        interceptPromotionalPurchases = true;
    }

    public func continuePromotionalPurchases() async {
        let options: [String: AnyObject] = [:]
        for productId in interceptedProductIds {
            _ = await self.purchaseProduct(productId: productId, options: options, storefrontChangeCallback: nil)
        }

        interceptedProductIds.removeAll()
    }
}

@available(iOS 15.0, macOS 12.0, *)
extension PurchaseUseCase: SKPaymentTransactionObserver {
    public func paymentQueue(_ queue: SKPaymentQueue, updatedTransactions transactions: [SKPaymentTransaction]) {

    }

    public func paymentQueue(_ queue: SKPaymentQueue, shouldAddStorePayment payment: SKPayment, for product: SKProduct) -> Bool {
        if (interceptPromotionalPurchases)
        {
            Task.detached(priority: .background, operation: {
                await self.storeKitCallback.callbackPtr(subject: "OnPromotionalPurchaseAttempted", payload: product.productIdentifier, entitlementStatus: 0)
            })
            interceptedProductIds.append(product.productIdentifier)
        }

        return !interceptPromotionalPurchases
    }
}
