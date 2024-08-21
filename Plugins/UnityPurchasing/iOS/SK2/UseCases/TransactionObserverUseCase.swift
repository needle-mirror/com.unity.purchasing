import Foundation
import StoreKit

/**
 This protocol observes the transactions and entitlements through the app lifecycle.
 It provide the interface to handle the cases when the product is purchased, verify the transaction, and update customer product status.
 */
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public protocol TransactionObserverUseCaseProtocol {
    func addTransactionObserver()
    func checkVerified<T>(_ result: VerificationResult<T>) throws -> T
    func updateCustomerProductStatus() async
    func updatePurchasedIdentifier(_ purchaseDetail: PurchaseDetails) async
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public class TransactionObserverUseCase: TransactionObserverUseCaseProtocol {
    @Dependency private(set) var storeKitCallback: StoreKitCallbackDelegate

    var updateListenerTask: Task<Void, Error>? = nil

    deinit {
        updateListenerTask?.cancel()
    }

    /**
     Start a transaction listener as close to app launch as possible so you don't miss any transactions.
     */
    public func addTransactionObserver() {
        updateListenerTask = listenForTransactions()
    }

    /**
     Verify the VerificationResult. StoreKit does the transaction verification for us.
     */
    public func checkVerified<T>(_ result: VerificationResult<T>) throws -> T {
        // Check whether the JWS passes StoreKit verification.
        switch result {
        case .unverified:
            // StoreKit parses the JWS, but it fails verification.
            throw StoreError.failedVerification
        case .verified(let safe):
            // The result is verified. Return the unwrapped value.
            return safe
        }
    }

    /**
     Iterate through all of the user's purchased products. Check whether the transaction is verified and notifiy the user that the product is purchased.
     If it isn’t, catch `failedVerification` error.
     */
    public func updateCustomerProductStatus() async {
        for await result in Transaction.currentEntitlements {
            do {
                _ = try checkVerified(result)
                await updatePurchasedIdentifier(result.purchaseDetails())
            } catch {
                printLog(error.localizedDescription)
            }
        }
    }

    public func updatePurchasedIdentifier(_ purchaseDetail: PurchaseDetails) async {
        let jsonString = encodeToJSON(purchaseDetail)
        await storeKitCallback.callback(subject: "OnPurchaseSucceeded", payload: jsonString, entitlementStatus: 0)
    }

    public func revokePurchase(_ purchaseDetail: PurchaseDetails) async {
        let jsonString = encodeToJSON(purchaseDetail)
        await storeKitCallback.callback(subject: "OnEntitlementRevoked", payload: jsonString, entitlementStatus: 0)
    }

    /**
     Iterate through any transactions that don't come from a direct call to `purchase()` because transactions can appear unexpectedly. This function will deliver unfinished transactions.
     */
    private func listenForTransactions() -> Task<Void, Error> {
        return Task.detached {
            for await result in Transaction.updates {
                do {
                    let transaction = try self.checkVerified(result)

                    if let _ = transaction.revocationDate {
                        // Remove access to the product identified by transaction.productID.
                        // Transaction.revocationReason provides details about
                        // the revoked transaction.
                        await self.revokePurchase(result.purchaseDetails())
                        await transaction.finish()

                    } else if let expirationDate = transaction.expirationDate,
                              expirationDate < Date() {
                        // Do nothing, this subscription is expired.
                        await transaction.finish()
                    } else if transaction.isUpgraded {
                        // Do nothing, there is an active transaction
                        // for a higher level of service.
                        await transaction.finish()
                    } else {
                        // Provide access to the product identified by
                        // transaction.productID.
                        await self.updatePurchasedIdentifier(result.purchaseDetails())
                    }
                } catch {
                    // StoreKit has a transaction that fails verification. Don't deliver content to the user.
                    printLog("Transaction failed verification.")
                }
            }
        }
    }
}
