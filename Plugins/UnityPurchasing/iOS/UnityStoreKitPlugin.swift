import Foundation

// Declared in C# as: delegate void CallbackDelegate(string subject, string payload);
public typealias UnityPurchasingCallbackDelegateType = @convention(c) (UnsafeMutablePointer<CChar>?, UnsafeMutablePointer<CChar>?, Int) -> Void
public typealias StorefrontCallbackDelegateType = @convention(c) (UnsafePointer<CChar>, UnsafePointer<CChar>) -> Bool

func printLog(_ message: String, file: String = #file, function: String = #function, line: Int = #line) {
        let className = file.components(separatedBy: "/").last
        print("Unity IAP: Function: \(function) File: \(className ?? "")\n\(message)")
}

/**
Set NativeCallback delegate from C#. The function is declared in C# as: static extern void
 - Parameters:
        - callback: A callback delegate that the native plugin will call in C#
 */
@_cdecl("unityPurchasing_SetNativeCallback")
@available(iOS 15.0, *)
public func unityPurchasing_SetNativeCallback(_ callback: UnityPurchasingCallbackDelegateType) {
    DependencyInjector.InitialiseWithCallback(callback)
}

/**
 Add TransactionObserver
 */
@_cdecl("unityPurchasing_AddTransactionObserver")
@available(iOS 15.0, *)
public func unityPurchasing_AddTransactionObserver() {
    StoreKitManager.instance.addTransactionObserver()
}

/**
 Fetch a list of products in json
 */
@available(iOS 15.0, *)
@_cdecl("unityPurchasing_FetchProducts")
public func unityPurchasing_FetchProducts(_ productJsonCString: UnsafePointer<CChar>) {
    let productJson = String(cString: productJsonCString)
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.fetchProducts(productJson: productJson)
    })
}

/**
 Purchase a product
 */
@available(iOS 15.0, *)
@_cdecl("unityPurchasing_PurchaseProduct")
public func unityPurchasing_PurchaseProduct(_ productJsonCString: UnsafePointer<CChar>, optionsJsonCString: UnsafePointer<CChar>) {
    let productJson = String(cString: productJsonCString)
    let optionsDict = dictionaryFromJSONCstr(optionsJsonCString) ?? [:]
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.purchase(productJson: productJson, options: optionsDict, storefrontChangeCallback: nil)
    })
}

/**
 Fetch receipt.
 - note The receipt isn’t necessary if you use AppTransaction to validate the app download, or Transaction to validate in-app purchases. Only use the receipt if your app uses the Original API for In-App Purchase, or needs the receipt to validate the app download because it can’t use AppTransaction.
 */
@available(iOS 15.0, *)
@_cdecl("unityPurchasing_FetchAppReceipt")
public func unityPurchasing_FetchAppReceipt() -> UnsafeMutablePointer<CChar>? {
    let receiptString = StoreKitManager.instance.fetchAppReceipt()
    return unityPurchasingMakeHeapAllocatedStringCopy(receiptString)
}

// Function to create a heap-allocated C string copy from a Swift string
func unityPurchasingMakeHeapAllocatedStringCopy(_ string: String?) -> UnsafeMutablePointer<CChar>? {
    guard let string = string else {
        return nil
    }

    // Convert the Swift string to a C string
    let utf8String = string.cString(using: .utf8)
    guard let utf8String = utf8String else {
        return nil
    }

    // Allocate memory on the heap for the C string, including space for the null terminator
    let length = utf8String.count
    let res = UnsafeMutablePointer<CChar>.allocate(capacity: length)

    // Initialize the allocated memory with the C string
    res.initialize(from: utf8String, count: length)

    return res
}

@_cdecl("unityPurchasing_DeallocateMemory")
// Function to deallocate the memory when done with the pointer
public func unityPurchasing_DeallocateMemory(_ pointer: UnsafeMutablePointer<CChar>?) {
    pointer?.deallocate()
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_FetchPurchases")
public func unityPurchasing_FetchPurchases() {
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.fetchPurchasedProducts()
    })
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_FetchTransactionForProductId")
public func unityPurchasing_FetchTransactionForProductId(_ productIdCString: UnsafePointer<CChar>) {
    let productId = String(cString: productIdCString)
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.fetchTransactions(for: [productId])
    })
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_CanMakePayments")
public func unityPurchasing_CanMakePayments() -> Bool {
    return StoreKitManager.instance.canMakePayment()
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_PresentCodeRedemptionSheet")
public func unityPurchasing_PresentCodeRedemptionSheet() {
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.presentCodeRedemptionSheet()
    })
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_RefreshAppReceipt")
public func unityPurchasing_RefreshAppReceipt() {
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.refreshAppReceipt()
    })
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_FinishTransaction")
public func unityPurchasing_FinishTransaction(transactionId: UnsafePointer<CChar>, logFinishTransaction: Bool) {
    if let transactionIdUInt64 = UInt64(String(cString: transactionId))
    {
        Task.detached(priority: .background, operation: {
            await StoreKitManager.instance.finishTransaction(transactionId: transactionIdUInt64, logFinishTransaction: logFinishTransaction)
        })
    }
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_checkEntitlement")
public func unityPurchasing_checkEntitlement(_ productJsonCString: UnsafePointer<CChar>) {
    let productId = String(cString: productJsonCString)
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.checkEntitlement(productId: productId)
    })
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_RestoreTransactions")
public func unityPurchasing_RestoreTransactions() {
    Task.detached (priority: .background, operation : {
        await StoreKitManager.instance.restoreTransactions()
    })
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_FetchStorePromotionOrder")
public func unityPurchasing_FetchStorePromotionOrder() {
    Task.detached (priority: .background, operation : {
        await StoreKitManager.instance.fetchStorePromotionOrder()
    })
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_UpdateStorePromotionOrder")
public func unityPurchasing_UpdateStorePromotionOrder(_ jsonCString: UnsafePointer<CChar>) {
    do {
        let jsonString = String(cString: jsonCString)

        let productIds = try decodeJSONToType(jsonString, [String].self)
        Task.detached (priority: .background, operation : {
            await StoreKitManager.instance.updateStorePromotionOrder(productIds: productIds)
        })
    } catch {
        printLog("JSONDecoder An error occurred - \(error.localizedDescription)")
    }
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_FetchStorePromotionVisibility")
public func unityPurchasing_FetchStorePromotionVisibility(productIdCString: UnsafePointer<CChar>) {
    let productId = String(cString: productIdCString)
    Task.detached (priority: .background, operation : {
        await StoreKitManager.instance.fetchStorePromotionVisibility(productId: productId)
    })
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_UpdateStorePromotionVisibility")
public func unityPurchasing_UpdateStorePromotionVisibility(productIdCString: UnsafePointer<CChar>, visibilityCString: UnsafePointer<CChar>) {
    let productId = String(cString: productIdCString)
    let visibility = String(cString: visibilityCString)
    Task.detached (priority: .background, operation : {
        await StoreKitManager.instance.updateStorePromotionVisibility(productId: productId, visibilityStr: visibility)
    })
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_InterceptPromotionalPurchases")
public func unityPurchasing_InterceptPromotionalPurchases() {
    Task.detached (priority: .background, operation : {
        await StoreKitManager.instance.interceptPromotionalPurchases()
    })
}

@available(iOS 15.0, *)
@_cdecl("unityPurchasing_ContinuePromotionalPurchases")
public func unityPurchasing_ContinuePromotionalPurchases() {
    Task.detached (priority: .background, operation : {
        await StoreKitManager.instance.continuePromotionalPurchases()
    })
}
