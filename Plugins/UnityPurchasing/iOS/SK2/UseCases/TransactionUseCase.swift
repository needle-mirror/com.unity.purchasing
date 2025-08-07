import Foundation
import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
// sourcery: AutoMockable
protocol TransactionUseCaseProtocol {
    func getPurchaseState(_ productId: String) async throws -> PurchaseState
    func isPending(_ productId: String) async throws -> PurchaseState
    func fetchAllTransactions() async -> (finishedTransactions: [String : PurchaseDetails], unfinishedTransactions: [String : PurchaseDetails])
    func fetchTransactions(for productIds: [String]) async -> TransactionResponse
    func finishTransaction(transactionId: UInt64, logFinishTransaction: Bool) async
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
class TransactionUseCase: TransactionUseCaseProtocol {
    @Dependency private(set) var transactionObserver: TransactionObserverUseCaseProtocol

    public func getPurchaseState(_ productId: String) async throws -> PurchaseState {
        guard let result = await Transaction.latest(for: productId) else {
            return PurchaseState.NotPurchased;
        }

        // Ensure we don't deliver content that is refunded by checking the `revocationDate` is nil
        let transaction = try transactionObserver.checkVerified(result)
        let isPending = await isPendingTransaction(transaction.id)

        let isPurchased = !isPending && transaction.revocationDate == nil && !transaction.isUpgraded && (transaction.expirationDate == nil || transaction.expirationDate! > Date())
        return isPurchased ? PurchaseState.Purchased : isPending ? PurchaseState.Pending : PurchaseState.NotPurchased
    }

    public func isPending(_ productId: String) async -> PurchaseState {
        // Fetch all unfinished transactions
        let transactions = Transaction.unfinished

        // Find the first verified transaction with the matching transactionId
        let transaction = await transactions.first(where: { result in
            if case .verified(let txn) = result {
                return txn.productID == productId
            }
            return false
        })

        return (transaction != nil) ? PurchaseState.Pending : PurchaseState.NotPurchased
    }

    public func isPendingTransaction(_ transactionId: UInt64) async -> Bool {
        // Fetch all unfinished transactions
        let transactions = Transaction.unfinished

        // Find the first verified transaction with the matching transactionId
        let transaction = await transactions.first(where: { result in
            if case .verified(let txn) = result {
                return txn.id == transactionId
            }
            return false
        })

        return transaction != nil
    }

    /**
     Use `Transaction.currentEntitlements` to fetch all purchases and `Transaction.unfinished` to fetch all pending purchases for the user
     */
    public func fetchAllTransactions() async -> (finishedTransactions: [String : PurchaseDetails], unfinishedTransactions: [String : PurchaseDetails]) {

        async let finishedTask = processCurrentEntitlements()
        async let unfinishedTask = processUnfinishedTransactions()

        let (finished, unfinished) = await (finishedTask, unfinishedTask)

        return (finishedTransactions: finished, unfinishedTransactions: unfinished)
    }

    private func processCurrentEntitlements() async -> [String: PurchaseDetails] {
        var result: [String: PurchaseDetails] = [:]

        for await transactionResult in Transaction.currentEntitlements {
            do {
                let transaction = try transactionObserver.checkVerified(transactionResult)
                let details = transactionResult.purchaseDetails()
                if let productId = details.productId {
                    result[productId] = details
                }
            } catch {
                printLog("Verification failed: \(error.localizedDescription)")
            }
        }

        return result
    }

    private func processUnfinishedTransactions() async -> [String: PurchaseDetails] {
        var result: [String: PurchaseDetails] = [:]

        for await transactionResult in Transaction.unfinished {
            do {
                let transaction = try transactionObserver.checkVerified(transactionResult)
                let details = transactionResult.purchaseDetails()
                if let productId = details.productId {
                    result[productId] = details
                }
            } catch {
                printLog("Verification failed: \(error.localizedDescription)")
            }
        }

        return result
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
