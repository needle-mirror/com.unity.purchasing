import Foundation
import StoreKit

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
// sourcery: AutoMockable
protocol TransactionUseCaseProtocol {
    func getPurchaseState(_ productId: String) async throws -> PurchaseState
    func isPending(_ productId: String) async throws -> PurchaseState
    func fetchAllTransactions() async -> (finishedTransactions: [String : PurchaseDetails], unfinishedTransactions: [String : PurchaseDetails])
    func fetchTransactions(for productIds: [String]) async -> TransactionResponse
    func finishTransaction(transactionId: UInt64, logFinishTransaction: Bool) async -> FinishTransactionResult
}

// Transactions delivered to C# as pending by `product.purchase()` or `Transaction.updates`, kept so a
// finish can reach one that `Transaction.unfinished` no longer lists. StoreKit can mark a transaction
// finished locally while the server never got the finish (e.g. offline): the App Store then answers
// each repeat purchase with that transaction, and only a `Transaction` instance can be finished again.
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
actor PurchasedTransactions {
    static let shared = PurchasedTransactions()
    private var transactions: [UInt64: Transaction] = [:]

    // Keyed by ID: a transaction delivered by more than one source is kept once.
    func keep(_ transaction: Transaction) {
        transactions[transaction.id] = transaction
    }

    func take(_ transactionId: UInt64) -> Transaction? {
        transactions.removeValue(forKey: transactionId)
    }
}

enum FinishTransactionResult {
    case finished
    case notFound
    // StoreKit could not finish it (e.g. offline) and it remains unfinished.
    case stillUnfinished
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
// @unchecked Sendable: required under Swift 6 strict concurrency as this type is resolved via the
// (@unchecked Sendable) DependencyContainer and used across async contexts. Safe because it is
// effectively immutable — it holds only a `@Dependency`; every method operates on local state.
class TransactionUseCase: TransactionUseCaseProtocol, @unchecked Sendable {
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

        let unfinishedTransactionIds: Set<UInt64> = Set(unfinished.values.compactMap { $0.transactionId })
        let filteredFinishedTransactions: [String: PurchaseDetails] = finished.filter { _, details in
            guard let txnId = details.transactionId else {
                // Drop entries without a transactionId
                return false
            }
            // Keep only if its transactionId is not present in unfinished
            return !unfinishedTransactionIds.contains(txnId)
        }

        return (finishedTransactions: filteredFinishedTransactions, unfinishedTransactions: unfinished)
    }

    private func processCurrentEntitlements() async -> [String: PurchaseDetails] {
        var result: [String: PurchaseDetails] = [:]

        for await transactionResult in Transaction.currentEntitlements {
            do {
                let transaction = try transactionObserver.checkVerified(transactionResult)
                let details = transactionResult.purchaseDetails()
                result[String(transaction.id)] = details
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

                if transaction.revocationDate != nil {
                    await transactionObserver.finishRevokedTransaction(transaction, transactionResult)
                    continue
                }
                if transaction.isUpgraded {
                    await transactionObserver.finishExpiredTransaction(transaction, transactionResult)
                    continue
                }
                if let expirationDate = transaction.expirationDate, expirationDate < Date() {
                    await transactionObserver.finishExpiredTransaction(transaction, transactionResult)
                    continue
                }

                let details = transactionResult.purchaseDetails()
                result[String(transaction.id)] = details
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
     - Returns: `.finished` if the transaction was found in `Transaction.unfinished` and finished,
     `.stillUnfinished` if it is still unfinished after `finish()`, `.notFound` if it was not found
     (StoreKit already considers it finished).
     */
    public func finishTransaction(transactionId: UInt64, logFinishTransaction: Bool) async -> FinishTransactionResult {
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
                _ = await PurchasedTransactions.shared.take(transactionId)
                // Finish the transaction
                await txn.finish()
                if (logFinishTransaction)
                {
                    printLog("Finishing transaction \(transactionId) \(txn.productID)")
                }
                // finish() cannot throw: when StoreKit fails to finish (e.g. NSURLErrorDomain -1009
                // while offline) it only logs, and the transaction stays unfinished. Online, the first
                // finish() sometimes does not take either, and a later one does: try twice more.
                var retries = 2
                while await isPendingTransaction(transactionId) {
                    if retries == 0 {
                        return .stillUnfinished
                    }
                    retries -= 1
                    try? await Task.sleep(nanoseconds: 500_000_000) // 0.5 s
                    printLog("Transaction \(transactionId) is still unfinished: finishing it again")
                    await txn.finish()
                }
                return .finished
            }
        }
        // Not unfinished locally, but it was delivered to C#: finish it again in case the server never
        // got the finish. Still reported as not found, so the C# outcome is unchanged.
        if let txn = await PurchasedTransactions.shared.take(transactionId) {
            await txn.finish()
            printLog("finishTransaction: \(transactionId) \(txn.productID) not found in unfinished transactions; finishing the delivered transaction.")
        }
        return .notFound
    }
}
