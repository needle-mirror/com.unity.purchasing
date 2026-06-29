import Foundation
#if canImport(UIKit)
import UIKit
#endif
#if os(iOS) || os(tvOS) || os(macOS)
import AdSupport
#endif

// Declared in C# as: delegate void CallbackDelegate(string subject, string payload);
public typealias UnityPurchasingCallbackDelegateType = @convention(c) (UnsafeMutablePointer<CChar>?, UnsafeMutablePointer<CChar>?, Int) -> Void
public typealias StorefrontCallbackDelegateType = @convention(c) (UnsafePointer<CChar>, UnsafePointer<CChar>) -> Bool
// Standalone callback for ExternalPurchaseClient — no entitlementStatus needed
public typealias ExternalPurchaseCallbackDelegateType = @convention(c) (UnsafeMutablePointer<CChar>?, UnsafeMutablePointer<CChar>?) -> Void

func printLog(_ message: String, file: String = #file, function: String = #function, line: Int = #line) {
        let className = file.components(separatedBy: "/").last
        print("Unity IAP: Function: \(function) File: \(className ?? "")\n\(message)")
}

/**
Set NativeCallback delegate from C#. The function is declared in C# as: static extern void
 - Parameters:
        - callback: A callback delegate that the native plugin will call in C#
 */
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_SetNativeCallback")
public func unityPurchasing_SetNativeCallback(_ callback: UnityPurchasingCallbackDelegateType) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    DependencyInjector.InitialiseWithCallback(callback)
}

/**
 Add TransactionObserver
 */
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_AddTransactionObserver")
public func unityPurchasing_AddTransactionObserver() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    StoreKitManager.instance.addTransactionObserver()
}

/**
 Fetch a list of products in json
 */
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_FetchProducts")
public func unityPurchasing_FetchProducts(_ productJsonCString: UnsafePointer<CChar>) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    let productJson = String(cString: productJsonCString)
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.fetchProducts(productJson: productJson)
    })
}

/**
 Purchase a product
 */
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_PurchaseProduct")
public func unityPurchasing_PurchaseProduct(_ productJsonCString: UnsafePointer<CChar>, optionsJsonCString: UnsafePointer<CChar>) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    let productJson = String(cString: productJsonCString)
    let optionsDict = dictionaryFromJSONCstr(optionsJsonCString) ?? [:]
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.purchase(productJson: productJson, options: optionsDict, storefrontChangeCallback: nil)
    })
}

/**
 Fetch receipt.
 - note The receipt isn't necessary if you use AppTransaction to validate the app download, or Transaction to validate in-app purchases. Only use the receipt if your app uses the Original API for In-App Purchase, or needs the receipt to validate the app download because it can't use AppTransaction.
 */
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_FetchAppReceipt")
public func unityPurchasing_FetchAppReceipt() -> UnsafeMutablePointer<CChar>? {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return nil }
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

/// Fetches device information fields that are not available from C# Unity APIs.
/// Returns a heap-allocated JSON string with keys.
/// Caller must deallocate with unityPurchasing_DeallocateMemory.
@_cdecl("unityPurchasing_FetchNativeDeviceInfo")
public func unityPurchasing_FetchNativeDeviceInfo() -> UnsafeMutablePointer<CChar>? {
    let language = Locale.preferredLanguages.first
    let localeList = Locale.preferredLanguages

    var bootTime = timeval()
    var size = MemoryLayout<timeval>.stride
    sysctlbyname("kern.boottime", &bootTime, &size, nil, 0)
    let systemBootTime = Int64(bootTime.tv_sec)

    var totalSpaceKB: Int64 = 0
    if let attrs = try? FileManager.default.attributesOfFileSystem(forPath: "/"),
       let totalBytes = attrs[.systemSize] as? Int64 {
        totalSpaceKB = totalBytes / 1024
    }

    var systemInfo = utsname()
    uname(&systemInfo)
    let deviceModel = withUnsafePointer(to: &systemInfo.machine) {
        $0.withMemoryRebound(to: CChar.self, capacity: 1) {
            String(cString: $0)
        }
    }

    var osVersion: String
    #if os(iOS) || os(tvOS) || os(visionOS)
    osVersion = UIDevice.current.systemVersion
    #else
    let v = ProcessInfo.processInfo.operatingSystemVersion
    osVersion = "\(v.majorVersion).\(v.minorVersion).\(v.patchVersion)"
    #endif

    var dict: [String: Any] = [
        "systemBootTime": systemBootTime,
        "totalSpace": totalSpaceKB,
        "localeList": localeList,
        "deviceModel": deviceModel,
        "osVersion": osVersion
    ]

    if let language = language {
        dict["language"] = language
    }

    guard let jsonData = try? JSONSerialization.data(withJSONObject: dict, options: []),
          let jsonString = String(data: jsonData, encoding: .utf8) else {
        return nil
    }

    return unityPurchasingMakeHeapAllocatedStringCopy(jsonString)
}

/// Formats `amount` as a currency string using the platform's CLDR data.
/// `locale` is a BCP-47 tag (e.g. "fr-CA"); `currencyCode` is an ISO 4217 code
/// (e.g. "USD"). Returns a heap-allocated string the caller must release with
/// unityPurchasing_DeallocateMemory. Returns nil on failure.
@_cdecl("unityPurchasing_FormatCurrency")
public func unityPurchasing_FormatCurrency(
    _ localePtr: UnsafePointer<CChar>?,
    _ currencyCodePtr: UnsafePointer<CChar>?,
    _ amount: Double
) -> UnsafeMutablePointer<CChar>? {
    guard let localePtr = localePtr, let currencyCodePtr = currencyCodePtr else { return nil }
    let localeId = String(cString: localePtr).replacingOccurrences(of: "-", with: "_")
    let currencyCode = String(cString: currencyCodePtr)
    let formatter = NumberFormatter()
    formatter.numberStyle = .currency
    formatter.locale = Locale(identifier: localeId)
    formatter.currencyCode = currencyCode
    return unityPurchasingMakeHeapAllocatedStringCopy(formatter.string(from: NSNumber(value: amount)))
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_FetchPurchases")
public func unityPurchasing_FetchPurchases() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.fetchPurchasedProducts()
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_FetchTransactionForProductId")
public func unityPurchasing_FetchTransactionForProductId(_ productIdCString: UnsafePointer<CChar>) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    let productId = String(cString: productIdCString)
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.fetchTransactions(for: [productId])
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_CanMakePayments")
public func unityPurchasing_CanMakePayments() -> Bool {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return false }
    return StoreKitManager.instance.canMakePayment()
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_PresentCodeRedemptionSheet")
public func unityPurchasing_PresentCodeRedemptionSheet() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.presentCodeRedemptionSheet()
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_RefreshAppReceipt")
public func unityPurchasing_RefreshAppReceipt() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.refreshAppReceipt()
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_FinishTransaction")
public func unityPurchasing_FinishTransaction(transactionId: UnsafePointer<CChar>, logFinishTransaction: Bool) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    if let transactionIdUInt64 = UInt64(String(cString: transactionId))
    {
        Task.detached(priority: .background, operation: {
            await StoreKitManager.instance.finishTransaction(transactionId: transactionIdUInt64, logFinishTransaction: logFinishTransaction)
        })
    }
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_checkEntitlement")
public func unityPurchasing_checkEntitlement(_ productJsonCString: UnsafePointer<CChar>) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    let productId = String(cString: productJsonCString)
    Task.detached(priority: .background, operation: {
        await StoreKitManager.instance.checkEntitlement(productId: productId)
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_RestoreTransactions")
public func unityPurchasing_RestoreTransactions() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached (priority: .background, operation : {
        await StoreKitManager.instance.restoreTransactions()
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_FetchStorePromotionOrder")
public func unityPurchasing_FetchStorePromotionOrder() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached (priority: .background, operation : {
        await StoreKitManager.instance.fetchStorePromotionOrder()
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_UpdateStorePromotionOrder")
public func unityPurchasing_UpdateStorePromotionOrder(_ jsonCString: UnsafePointer<CChar>) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
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

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_FetchStorePromotionVisibility")
public func unityPurchasing_FetchStorePromotionVisibility(productIdCString: UnsafePointer<CChar>) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    let productId = String(cString: productIdCString)
    Task.detached (priority: .background, operation : {
        await StoreKitManager.instance.fetchStorePromotionVisibility(productId: productId)
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_UpdateStorePromotionVisibility")
public func unityPurchasing_UpdateStorePromotionVisibility(productIdCString: UnsafePointer<CChar>, visibilityCString: UnsafePointer<CChar>) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    let productId = String(cString: productIdCString)
    let visibility = String(cString: visibilityCString)
    Task.detached (priority: .background, operation : {
        await StoreKitManager.instance.updateStorePromotionVisibility(productId: productId, visibilityStr: visibility)
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_InterceptPromotionalPurchases")
public func unityPurchasing_InterceptPromotionalPurchases() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached (priority: .background, operation : {
        await StoreKitManager.instance.interceptPromotionalPurchases()
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_ContinuePromotionalPurchases")
public func unityPurchasing_ContinuePromotionalPurchases() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached (priority: .background, operation : {
        await StoreKitManager.instance.continuePromotionalPurchases()
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_FetchStorefront")
public func unityPurchasing_FetchStorefront() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached (priority: .background, operation : {
        await StoreKitManager.instance.fetchStorefront()
    })
}

// MARK: - Advertising Identifier

@_cdecl("unityPurchasing_FetchAdvertisingIdentifier")
public func unityPurchasing_FetchAdvertisingIdentifier() -> UnsafeMutablePointer<CChar>? {
    var idfa: String?
#if os(iOS) || os(tvOS)
    idfa = ASIdentifierManager.shared().advertisingIdentifier.uuidString
#elseif os(macOS)
    if #available(macOS 13.1, *) {
        idfa = ASIdentifierManager.shared().advertisingIdentifier.uuidString
    }
#endif
    return unityPurchasingMakeHeapAllocatedStringCopy(idfa)
}

@_cdecl("unityPurchasing_FetchVendorIdentifier")
public func unityPurchasing_FetchVendorIdentifier() -> UnsafeMutablePointer<CChar>? {
    var idfv: String?
#if os(iOS) || os(tvOS)
    idfv = UIDevice.current.identifierForVendor?.uuidString
#endif
    return unityPurchasingMakeHeapAllocatedStringCopy(idfv)
}

// MARK: - ExternalPurchaseCustomLink (standalone — callback pointer per call)

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("externalPurchase_CheckEligibility")
public func externalPurchase_CheckEligibility(_ callback: @escaping ExternalPurchaseCallbackDelegateType) {
    Task.detached(priority: .background, operation: {
        await ExternalPurchaseStandalone.checkEligibility(callback: callback)
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("externalPurchase_FetchToken")
public func externalPurchase_FetchToken(_ tokenTypeCString: UnsafePointer<CChar>, _ callback: @escaping ExternalPurchaseCallbackDelegateType) {
    let tokenType = String(cString: tokenTypeCString)
    Task.detached(priority: .background, operation: {
        await ExternalPurchaseStandalone.fetchToken(tokenType: tokenType, callback: callback)
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("externalPurchase_ShowNotice")
public func externalPurchase_ShowNotice(_ noticeTypeCString: UnsafePointer<CChar>, _ callback: @escaping ExternalPurchaseCallbackDelegateType) {
    let noticeType = String(cString: noticeTypeCString)
    Task.detached(priority: .background, operation: {
        await ExternalPurchaseStandalone.showNotice(noticeType: noticeType, callback: callback)
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("externalPurchase_FetchStorefront")
public func externalPurchase_FetchStorefront(_ callback: @escaping ExternalPurchaseCallbackDelegateType) {
    Task.detached(priority: .background, operation: {
        await ExternalPurchaseStandalone.fetchStorefront(callback: callback)
    })
}
