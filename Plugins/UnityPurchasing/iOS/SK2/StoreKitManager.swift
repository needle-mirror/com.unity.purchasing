import Foundation
import StoreKit

public enum StoreError: Error {
    case failedVerification
}

public enum PurchaseState: Int {
    case NotPurchased = 0
    case Purchased = 1
    case Pending = 2
}

/**
 This protocol acts as a main interface for the StoreKit framework to provide the following features and services for your apps and In-App Purchases.
 For more information, see [The Storekit2 Documentation](https://developer.apple.com/documentation/storekit)
 */
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public protocol StoreKitManagerProtocol {
    func canMakePayment() -> Bool
    func addTransactionObserver()
    func fetchProducts(productJson: String) async
    func fetchSubscriptionInfo(for productId: String) async
    func purchase(productJson: String, options: [String: AnyObject], storefrontChangeCallback: StorefrontCallbackDelegateType?) async
    func fetchAppReceipt() -> String
    func fetchPurchasedProducts() async
    func fetchTransactions(for productIds: [String]) async
    func finishTransaction(transactionId: UInt64, logFinishTransaction: Bool) async
    func checkEntitlement(productId: String) async
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public class StoreKitManager: StoreKitManagerProtocol {
    @Dependency private(set) var productUseCase: ProductUseCaseProtocol
    @Dependency private(set) var purchaseUseCase: PurchaseUseCaseProtocol
    @Dependency private(set) var transactionObserver: TransactionObserverUseCaseProtocol
    @Dependency private(set) var transactionUseCase: TransactionUseCaseProtocol
    @Dependency private(set) var storeKitCallback: StoreKitCallbackDelegate

    private(set) var products: [Product] = []
    var purchasedProducts: [Product] = []
    var receiptData: Data?

    // MARK: Singleton
    static let instance: StoreKitManager = {
        let sharedInstance = StoreKitManager()
        return sharedInstance
    }()

    // MARK: TransactionObserver

    /**
     Add transaction observer to listen to the purchased transactions in the app life cycle
     */
    public func addTransactionObserver() {
        transactionObserver.addTransactionObserver()
#if os(macOS) || os(iOS)
        if #available(iOS 16.4, macOS 14.4, *) {
            purchaseUseCase.addPurchaseIntentListener()
        }
#endif
    }

    // MARK: Purchases

    /**
     Purchase a product
     - Parameter productId: productId as specified in App Store Connect
     - Parameter options: purchase options
     - Parameter storefrontChangeCallback: handler for result
     */
    public func purchase(productJson: String, options: [String: AnyObject], storefrontChangeCallback: StorefrontCallbackDelegateType?) async {
        var productId = ""
        do {
            let product = try decodeJSONToType(productJson, ProductDefinition.self)
            productId = product.storeSpecificId
            guard let purchaseDetail = await purchaseUseCase.purchaseProduct(productId: productId, options: options, storefrontChangeCallback: storefrontChangeCallback)
            else {
                return
            }
            let jsonString = encodeToJSON(purchaseDetail)
            await storeKitCallback.callback(subject: "OnPurchaseSucceeded", payload: jsonString, entitlementStatus: 0)
        } catch {
            let purchaseDetail = PurchaseDetails(productId: productId, verificationError: error.localizedDescription, reason: 7 /* 7 = Unknown */)
            let jsonString = encodeToJSON(purchaseDetail)
            await self.storeKitCallback.callback(subject: "OnPurchaseFailed", payload: jsonString, entitlementStatus: 0)
        }
    }

    // MARK: Products

    /**
        Fetch products using productId as as specified in App Store Connect and call the delegate when it received a response from the API.
        - Parameter productIds: A list of productId
     */
    public func fetchProducts(productJson: String) async {
        do {
            let product = try decodeJSONToType(productJson, [ProductDefinition].self)
            let storeSpecificIds = product.map { $0.storeSpecificId }

            let response = await productUseCase.fetchProducts(for: storeSpecificIds)
            products = response.products
            let jsonString = encodeToJSON( ["products": products])
            await storeKitCallback.callback(subject: "OnProductsFetched", payload: jsonString, entitlementStatus: 0)
        } catch {
            Task(priority: .background, operation: {
                await self.storeKitCallback.callback(subject: "OnProductsFetchFailed", payload: "JSONDecoder An error occurred - \(error.localizedDescription)", entitlementStatus: 0)
            })
        }
    }

    // MARK: Subscriptions

    /**
     Fetch subscription with the productId
     - Parameter productId: StoreKit Product Identifier
     */
    public func fetchSubscriptionInfo(for productId: String) async {
        let response = await productUseCase.fetchSubscribtion(for: productId)
        await storeKitCallback.callback(subject: "OnSubscriptionInfoStatusRetrieved", payload: response.description, entitlementStatus: 0)
    }

    // MARK: Receipts

    /**
     Fetch the receipt data from the application bundle. This is read from `Bundle.main.appStoreReceiptURL`.

     - Warning:
        The method isn’t necessary because we use Transaction to validate in-app purchases. It is here to support IAP Package that expects the receipt payload.
     */
    public func fetchAppReceipt() -> String {
        if let appStoreReceiptURL = Bundle.main.appStoreReceiptURL,
            FileManager.default.fileExists(atPath: appStoreReceiptURL.path) {
            do {
                receiptData = try Data(contentsOf: appStoreReceiptURL, options: .alwaysMapped)
                guard let receiptString = receiptData?.base64EncodedString(options: [.endLineWithCarriageReturn]) else {
                    return ""
                }
                return receiptString
            }
            catch {
                printLog("Couldn't read receipt data with error: " + error.localizedDescription)
                return ""
            }
        } else {
            printLog("receipt not available")
            return ""
        }
    }

    // TODO: IAP-3929 - Remove refreshAppReceipt
    func refreshAppReceipt() async {
        enum ReceiptRefreshError: Error {
            case requestThrottled
        }

        do {
            try await withCheckedThrowingContinuation { (continuation: CheckedContinuation<Void, Error>) in

                class RequestDelegate: NSObject, SKRequestDelegate {
                    private let continuation: CheckedContinuation<Void, Error>
                    private let request: SKRequest

                    init(request: SKRequest, continuation: CheckedContinuation<Void, Error>) {
                        self.request = request
                        self.continuation = continuation
                    }

                    func requestDidFinish(_ request: SKRequest) {
                        finish(success: true, error: nil)
                    }

                    func request(_ request: SKRequest, didFailWithError error: Error) {
                        if let skerror = error as? SKError, skerror.code.rawValue == 603 {
                            finish(success: false, error: ReceiptRefreshError.requestThrottled)
                        } else {
                            finish(success: false, error: error)
                        }
                    }

                    private func finish(success: Bool, error: Error?) {
                        request.cancel()

                        objc_setAssociatedObject(request, &AssociatedKeys.delegateKey, nil, .OBJC_ASSOCIATION_RETAIN_NONATOMIC)

                        if success || (error as? ReceiptRefreshError == .requestThrottled) {
                            continuation.resume()
                        } else {
                            continuation.resume(throwing: error!)
                        }
                    }
                }

                struct AssociatedKeys { static var delegateKey = 0 }

                enum ReceiptRefreshError: Error {
                    case requestThrottled
                }

                let request = SKReceiptRefreshRequest()
                let delegate = RequestDelegate(request: request, continuation: continuation)
                request.delegate = delegate

                objc_setAssociatedObject(request, &AssociatedKeys.delegateKey, delegate, .OBJC_ASSOCIATION_RETAIN_NONATOMIC)
                request.start()
            }

            // Successful refresh or receipt throttled, (valid receipt either way explicitly stated):
            await storeKitCallback.callback(
                subject: "onAppReceiptRefreshed",
                payload: fetchAppReceipt(),
                entitlementStatus: 0
            )

        } catch {
            // Error explicitly handled:
            await storeKitCallback.callback(
                subject: "onAppReceiptRefreshFailed",
                payload: error.localizedDescription,
                entitlementStatus: 0
            )
        }
    }


    // MARK: Transaction

    /**
     Check whether a productId is purchased
     - Parameter productId: StoreKit Product Identifier
     */
    public func getPurchaseState(_ productId: String) async throws -> PurchaseState {
        return try await transactionUseCase.getPurchaseState(productId)
    }

    /**
     Fetch the products that the users have purchased.
     */
    public func fetchPurchasedProducts() async {
        let response = await transactionUseCase.fetchAllTransactions()
        let result = TransactionsResult(
            finishedTransactions: response.finishedTransactions,
            unfinishedTransactions: response.unfinishedTransactions
        )

        var encodedResult = Data()
        do {
            encodedResult = try JSONEncoder().encode(result)
        }
        catch {
            printLog("fetchPurchasedProducts: Encoding purchases failed.")
        }

        let jsonString = String(data: encodedResult, encoding: .utf8) ?? ""

        await storeKitCallback.callback(subject: "OnPurchasesFetched", payload: jsonString, entitlementStatus: 0)
    }

    struct TransactionsResult: Encodable {
        let finishedTransactions: [String : PurchaseDetails]
        let unfinishedTransactions: [String : PurchaseDetails]
    }

    /**
     Fetch the transaction of a list of product identifiers
     */
    public func fetchTransactions(for productIds: [String]) async {
        let transactionResponse = await transactionUseCase.fetchTransactions(for: productIds)
        let jsonString = encodeToJSON(transactionResponse.purchases)
        await storeKitCallback.callback(subject: "OnPurchasesFetched", payload: jsonString, entitlementStatus: 0)
    }

    /**
     Indicates to the App Store that the app delivered the purchased content or enabled the service to finish the transaction.
     - Parameter transactionId: TransactionIdentifier
     */
    public func finishTransaction(transactionId: UInt64, logFinishTransaction: Bool) async
    {
        await transactionUseCase.finishTransaction(transactionId: transactionId, logFinishTransaction: logFinishTransaction)
    }

    /**
     Checks the entitlement status for the specified product.
     - Parameter productId: productIdentifier
     */
    public func checkEntitlement(productId: String) async
    {
        var result = PurchaseState.NotPurchased;
        do {
            result = try await getPurchaseState(productId)
        } catch {
            printLog("checkEntitlement error - \(error.localizedDescription)")
        }

        await storeKitCallback.callback(subject: "OnCheckEntitlement", payload: productId, entitlementStatus: result.rawValue)
    }

    public func canMakePayment() -> Bool {
        return AppStore.canMakePayments
    }

    public func presentCodeRedemptionSheet() async {
#if !os(macOS)
        if #available(iOS 16, *) {
            do {
                if let windowScene = await UIApplication.shared.connectedScenes.first as? UIWindowScene {
                    try await StoreKit.AppStore.presentOfferCodeRedeemSheet(in: windowScene)
                }
            }
            catch {
                printLog("Error occured while presenting Code Redemption Sheet: \(error)")
            }
        } else {
            SKPaymentQueue.default().presentCodeRedemptionSheet()
        }
#elseif os(macOS)
        printLog("Offer Code redemption is unavailable on macOS")
#endif
    }

    public func restoreTransactions() async
    {
        do {
            try await AppStore.sync()
            await storeKitCallback.callback(subject: "OnTransactionsRestoredSuccess", payload: "", entitlementStatus: 0)
        }
        catch {
            await storeKitCallback.callback(subject: "OnTransactionsRestoredFail", payload: error.localizedDescription, entitlementStatus: 0)
        }
    }

    public func fetchStorePromotionOrder() async {
#if os(iOS)
        if #available(iOS 16.4, *) {
            do {
                let promotions = try await Product.PromotionInfo.currentOrder
                let productIdentifiers = promotions.map { $0.productID }
                let payload = encodeToJSON(productIdentifiers)

                await self.storeKitCallback.callback(subject: "OnFetchStorePromotionOrderSucceeded", payload: payload, entitlementStatus: 0)
            }
            catch {
                await self.storeKitCallback.callback(subject: "OnFetchStorePromotionOrderFailed", payload: error.localizedDescription, entitlementStatus: 0)
            }
        } else {
            await self.storeKitCallback.callback(subject: "OnFetchStorePromotionOrderFailed", payload: "Fetch store promotion order is only available on iOS 16.4 and above", entitlementStatus: 0)
        }
#else
        await self.storeKitCallback.callback(subject: "OnFetchStorePromotionOrderFailed", payload: "Fetch store promotion order is only available on iOS 16.4 and above", entitlementStatus: 0)
#endif
    }

    public func updateStorePromotionOrder(productIds: [String]) async
    {
#if os(iOS)
        if #available(iOS 16.4, *) {
            do {
                try await Product.PromotionInfo.updateProductOrder(byID: productIds)
            }
            catch {
                printLog("Update store promotion order error: \(error.localizedDescription)")
            }
        } else {
            printLog("Update store promotion order is only available on iOS 16.4 and above")
        }
#else
        printLog("Update store promotion order is only available on iOS 16.4 and above")
#endif
    }

    public func fetchStorePromotionVisibility(productId: String) async
    {
#if os(iOS)
        if #available(iOS 16.4, *) {
            do {
                let promotionInfos = try await Product.PromotionInfo.currentOrder
                let promotionInfo = promotionInfos.first(where: { $0.productID == productId })

                let jsonString = encodeToJSON(promotionInfo)


                await storeKitCallback.callback(subject: "OnFetchStorePromotionVisibilitySucceeded", payload: jsonString, entitlementStatus: 0)
            }
            catch {
                await storeKitCallback.callback(subject: "OnFetchStorePromotionVisibilityFailed", payload: "Fetch store promotion visibility error: \(error.localizedDescription)", entitlementStatus: 0)
            }
        } else {
            await storeKitCallback.callback(subject: "OnFetchStorePromotionVisibilityFailed", payload: "Fetch store promotion visibility is only available on iOS 16.4 and above", entitlementStatus: 0)
        }
#else
        await storeKitCallback.callback(subject: "OnFetchStorePromotionVisibilityFailed", payload: "Fetch store promotion visibility is only available on iOS 16.4 and above", entitlementStatus: 0)
#endif
    }

    public func updateStorePromotionVisibility(productId: String, visibilityStr: String) async
    {
#if os(iOS)
        if #available(iOS 16.4, *) {
            do {
                let visibility: Product.PromotionInfo.Visibility
                switch visibilityStr {
                case "AppStoreConnectDefault":
                    visibility = Product.PromotionInfo.Visibility.appStoreConnectDefault
                case "Hidden":
                    visibility = Product.PromotionInfo.Visibility.hidden
                case "Visible":
                    visibility = Product.PromotionInfo.Visibility.visible
                default:
                    throw PromotionVisibilityError.invalidVisibility(value: visibilityStr, reason:"Invalid visibility value: \(visibilityStr)")
                }

                try await Product.PromotionInfo.updateProductVisibility(visibility, for: productId)
            }
            catch {
                printLog("Update store promotion visibility error: \(error.localizedDescription)")
            }
        } else {
            printLog("Update store promotion visibility is only available on iOS 16.4 and above")
        }
#else
        printLog("Update store promotion visibility is only available on iOS 16.4 and above")
#endif
    }

    enum PromotionVisibilityError: Error {
        case invalidVisibility(value: String, reason: String)
    }

    public func interceptPromotionalPurchases() async
    {
        purchaseUseCase.activateInterceptPromotionalPurchases()
    }

    public func continuePromotionalPurchases() async
    {
        await purchaseUseCase.continuePromotionalPurchases()
    }
}
