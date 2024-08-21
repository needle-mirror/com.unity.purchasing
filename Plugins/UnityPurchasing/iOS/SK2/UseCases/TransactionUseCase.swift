import Foundation
import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
// sourcery: AutoMockable
protocol TransactionUseCaseProtocol {
    func isPurchased(_ productId: String) async throws -> Bool
    func isPending(_ productId: String) async throws -> Bool
    func fetchAllTransactions() async -> (finishedTransactions: [String : PurchaseDetails], unfinishedTransactions: [String : PurchaseDetails])
    func fetchTransactions(for productIds: [String]) async -> TransactionResponse
    func finishTransaction(transactionId: UInt64, logFinishTransaction: Bool) async
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
class TransactionUseCase: TransactionUseCaseProtocol {
    @Dependency private(set) var transactionObserver: TransactionObserverUseCaseProtocol

    public func isPurchased(_ productId: String) async throws -> Bool {
        guard let result = await Transaction.latest(for: productId) else {
            return false
        }

        // Ensure we don't deliver content that is refunded by checking the `revocationDate` is nil
        let transaction = try transactionObserver.checkVerified(result)
        let isPurchased = transaction.revocationDate == nil && !transaction.isUpgraded && (transaction.expirationDate == nil || transaction.expirationDate! > Date())
        return isPurchased
    }

    public func isPending(_ productId: String) async -> Bool {
        // Fetch all unfinished transactions
        let transactions = Transaction.unfinished

        // Find the first verified transaction with the matching transactionId
        let transaction = await transactions.first(where: { result in
            if case .verified(let txn) = result {
                return txn.productID == productId
            }
            return false
        })

        return transaction != nil
    }

    /**
     Use `Transaction.currentEntitlements` to fetch all purchases and `Transaction.unfinished` to fetch all pending purchases for the user
     */
    public func fetchAllTransactions() async -> (finishedTransactions: [String : PurchaseDetails], unfinishedTransactions: [String : PurchaseDetails]) {
        var finishedTransactions: [String: PurchaseDetails] = [:]
        var unfinishedTransactions: [String: PurchaseDetails] = [:]

        // Fetch current entitlements
        for await result in Transaction.currentEntitlements {
            do {
                let transaction = try transactionObserver.checkVerified(result)
                if await (try isPurchased(transaction.productID))
                {
                    let details = result.purchaseDetails()
                    finishedTransactions[details.productId!] = details
                }
            } catch {
                printLog("Verification failed: \(error.localizedDescription)")
            }
        }

        // Fetch unfinished transactions
        for await result in Transaction.unfinished {
            do {
                let transaction = try transactionObserver.checkVerified(result)
                if await (isPending(transaction.productID))
                {
                    let details = result.purchaseDetails()
                    unfinishedTransactions[details.productId!] = details
                }
            } catch {
                printLog("Verification failed: \(error.localizedDescription)")
            }
        }

        return (finishedTransactions: finishedTransactions, unfinishedTransactions: unfinishedTransactions)
    }


    /**
     Fetch the latest transaction for the list of product Identifiers.
     */
    public func fetchTransactions(for productIds: [String]) async -> TransactionResponse {
        var transactions: Array<VerificationResult<Transaction>> = []
        var failures: Array<String> = []

        for productId in productIds {
            await fetchTransaction(for: productId, transactions: &transactions, failures: &failures)
        }

        return TransactionResponse(successes: transactions, failures: failures)
    }

    private func fetchTransaction(for productId: String, transactions: inout Array<VerificationResult<Transaction>>, failures: inout Array<String>) async {
        if let transaction = await Transaction.latest(for: productId) {
            transactions.append(transaction)
        } else {
            failures.append(productId)
        }
    }

    /**
     Use `Transaction.finish` to finish the specified transaction.
     */
    public func finishTransaction(transactionId: UInt64, logFinishTransaction: Bool) async {
        // Fetch all unfinished transactions
        let transactions = Transaction.unfinished

        // Find the first verified transaction with the matching transactionId
        if let transaction = await transactions.first(where: { result in
            if case .verified(let txn) = result {
                return txn.id == transactionId
            }
            return false
        }) {
            // Extract the verified transaction
            if case .verified(let txn) = transaction {
                // Finish the transaction
                await txn.finish()
                if (logFinishTransaction)
                {
                    printLog("Finishing transaction \(transactionId) \(txn.productID)")
                }
            }
        }
    }
}
