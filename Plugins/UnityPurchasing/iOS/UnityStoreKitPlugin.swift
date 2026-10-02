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

#if os(iOS) || os(tvOS) || os(visionOS)
/// Device values UIKit publishes on the main actor, served to the nonisolated `@_cdecl` entry
/// points below. Unity does not guarantee which thread it calls those on, so they must not touch
/// `UIDevice` directly: under Swift 6 that is a main-actor violation, and asserting isolation
/// there would trap rather than warn if the assumption were ever wrong.
///
/// Primed at initialisation, and again on any main-thread read while a value is still missing. A
/// read before priming falls back to reading directly, but only after confirming the main thread,
/// so the assertion below can never trap.
///
/// Each value tracks its own availability rather than sharing one flag. `identifierForVendor` is
/// nil until the first unlock after a reboot, and a nil must never be recorded as the final answer:
/// doing so would stop every later read from retrying once one becomes available.
final class UnityPurchasingDeviceValues: @unchecked Sendable {
    static let shared = UnityPurchasingDeviceValues()

    private let lock = NSLock()
    private var systemVersion: String?
    private var vendorIdentifier: String?

    @MainActor
    func prime() {
        let version = UIDevice.current.systemVersion
        let identifier = UIDevice.current.identifierForVendor?.uuidString
        lock.lock()
        systemVersion = version
        // Only record a real identifier. A nil here means it is not available yet, not that there
        // is none, so leaving the slot empty keeps later reads retrying.
        if identifier != nil {
            vendorIdentifier = identifier
        }
        lock.unlock()
    }

    var osVersion: String? {
        primeIfOnMainThread()
        lock.lock()
        defer { lock.unlock() }
        return systemVersion
    }

    var idfv: String? {
        primeIfOnMainThread()
        lock.lock()
        defer { lock.unlock() }
        return vendorIdentifier
    }

    var isPrimed: Bool {
        lock.lock()
        defer { lock.unlock() }
        return systemVersion != nil
    }

    private var isMissingAValue: Bool {
        lock.lock()
        defer { lock.unlock() }
        return systemVersion == nil || vendorIdentifier == nil
    }

    /// Primes only from the main thread, so the isolation assertion below cannot trap. Checking the
    /// thread is what makes `assumeIsolated` safe here: unchecked, it would abort the process.
    ///
    /// Retries while any value is still missing rather than once overall, so an identifier that was
    /// unavailable at the first read is picked up later. The reads themselves are cheap.
    func primeIfOnMainThread() {
        guard isMissingAValue, Thread.isMainThread else {
            return
        }
        MainActor.assumeIsolated { prime() }
    }

    /// Schedules a prime for callers that reached a missing value off the main thread, where
    /// reading UIKit directly is not permitted. Does not help the current call, which still falls
    /// back, but means the next one is served from the cache.
    func schedulePrimeIfNeeded() {
        guard isMissingAValue else {
            return
        }
        Task { @MainActor in prime() }
    }
}
#endif

/**
Set NativeCallback delegate from C#. The function is declared in C# as: static extern void
 - Parameters:
        - callback: A callback delegate that the native plugin will call in C#
 */
@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_SetNativeCallback")
public func unityPurchasing_SetNativeCallback(_ callback: UnityPurchasingCallbackDelegateType) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
#if os(iOS) || os(tvOS) || os(visionOS)
    // Prime the device values here, at initialisation, rather than during a purchase. Unity calls
    // this on the main thread in practice, in which case priming completes before this returns and
    // there is no window at all; the Task is the backup for when it does not.
    UnityPurchasingDeviceValues.shared.primeIfOnMainThread()
    if !UnityPurchasingDeviceValues.shared.isPrimed {
        Task { @MainActor in UnityPurchasingDeviceValues.shared.prime() }
    }
#endif
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
    Task.detached(priority: .medium, operation: {
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
    Task.detached(priority: .userInitiated, operation: {
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
    if let cached = UnityPurchasingDeviceValues.shared.osVersion {
        osVersion = cached
    } else {
        // Only reachable when called off the main thread before initialisation primed the cache,
        // which the standalone Payment Provider path can do: it builds native bindings without
        // registering the callback that primes, so nothing has run on the main actor yet.
        //
        // ProcessInfo always reports three components where UIDevice drops a zero patch, so the
        // shape is matched here deliberately. Reporting "18.5.0" where every other record says
        // "18.5" would split the same OS across two values in telemetry.
        let v = ProcessInfo.processInfo.operatingSystemVersion
        osVersion = v.patchVersion == 0
            ? "\(v.majorVersion).\(v.minorVersion)"
            : "\(v.majorVersion).\(v.minorVersion).\(v.patchVersion)"
        // Cannot read UIKit from here, so ask the main actor to fill the cache for the next caller.
        UnityPurchasingDeviceValues.shared.schedulePrimeIfNeeded()
        printLog("Device values were not primed before the first read; reporting osVersion \(osVersion) from ProcessInfo.")
    }
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

/// Returns the device's current locale as heap-allocated JSON, e.g.
/// {"countryCode": "NG", "localeIdentifier": "en_NG", "currencyCode": "NGN"}.
/// Used instead of .NET System.Globalization, whose Mono implementation collapses
/// locales without a Windows LCID to en-US (UUM-151469).
/// Caller must deallocate with unityPurchasing_DeallocateMemory.
@_cdecl("unityPurchasing_GetDeviceLocale")
public func unityPurchasing_GetDeviceLocale() -> UnsafeMutablePointer<CChar>? {
    let locale = Locale.current
    var dict: [String: Any] = ["localeIdentifier": locale.identifier]
    if #available(iOS 16.0, macOS 13.0, tvOS 16.0, visionOS 1.0, *) {
        if let region = locale.region?.identifier {
            dict["countryCode"] = region
        }
        if let currency = locale.currency?.identifier {
            dict["currencyCode"] = currency
        }
    } else {
        if let region = locale.regionCode {
            dict["countryCode"] = region
        }
        if let currency = locale.currencyCode {
            dict["currencyCode"] = currency
        }
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
    Task.detached(priority: .medium, operation: {
        await StoreKitManager.instance.fetchPurchasedProducts()
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_FetchTransactionForProductId")
public func unityPurchasing_FetchTransactionForProductId(_ productIdCString: UnsafePointer<CChar>) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    let productId = String(cString: productIdCString)
    Task.detached(priority: .medium, operation: {
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
    Task.detached(priority: .userInitiated, operation: {
        await StoreKitManager.instance.presentCodeRedemptionSheet()
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_RefreshAppReceipt")
public func unityPurchasing_RefreshAppReceipt() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached(priority: .userInitiated, operation: {
        await StoreKitManager.instance.refreshAppReceipt()
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_FinishTransaction")
public func unityPurchasing_FinishTransaction(transactionId: UnsafePointer<CChar>, logFinishTransaction: Bool) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    let transactionIdString = String(cString: transactionId)
    Task.detached(priority: .userInitiated, operation: {
        if let transactionIdUInt64 = UInt64(transactionIdString)
        {
            await StoreKitManager.instance.finishTransaction(transactionId: transactionIdUInt64, logFinishTransaction: logFinishTransaction)
        }
        else
        {
            printLog("finishTransaction: transactionId \(transactionIdString) is not a valid UInt64.")
            await StoreKitManager.instance.storeKitCallback.callback(subject: "OnFinishTransactionFailed", payload: transactionIdString, entitlementStatus: 0)
        }
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_checkEntitlement")
public func unityPurchasing_checkEntitlement(_ productJsonCString: UnsafePointer<CChar>) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    let productId = String(cString: productJsonCString)
    Task.detached(priority: .medium, operation: {
        await StoreKitManager.instance.checkEntitlement(productId: productId)
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_RestoreTransactions")
public func unityPurchasing_RestoreTransactions() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached (priority: .userInitiated, operation : {
        await StoreKitManager.instance.restoreTransactions()
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_FetchStorePromotionOrder")
public func unityPurchasing_FetchStorePromotionOrder() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached (priority: .medium, operation : {
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
        Task.detached (priority: .medium, operation : {
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
    Task.detached (priority: .medium, operation : {
        await StoreKitManager.instance.fetchStorePromotionVisibility(productId: productId)
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_UpdateStorePromotionVisibility")
public func unityPurchasing_UpdateStorePromotionVisibility(productIdCString: UnsafePointer<CChar>, visibilityCString: UnsafePointer<CChar>) {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    let productId = String(cString: productIdCString)
    let visibility = String(cString: visibilityCString)
    Task.detached (priority: .medium, operation : {
        await StoreKitManager.instance.updateStorePromotionVisibility(productId: productId, visibilityStr: visibility)
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_InterceptPromotionalPurchases")
public func unityPurchasing_InterceptPromotionalPurchases() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached (priority: .medium, operation : {
        await StoreKitManager.instance.interceptPromotionalPurchases()
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_ContinuePromotionalPurchases")
public func unityPurchasing_ContinuePromotionalPurchases() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached (priority: .userInitiated, operation : {
        await StoreKitManager.instance.continuePromotionalPurchases()
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("unityPurchasing_FetchStorefront")
public func unityPurchasing_FetchStorefront() {
    guard #available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *) else { return }
    Task.detached (priority: .medium, operation : {
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
    idfv = UnityPurchasingDeviceValues.shared.idfv
    if idfv == nil {
        // Either not yet primed, or the identifier is genuinely unavailable until the first unlock
        // after a reboot. Either way the next read should find it, so prime off this one.
        UnityPurchasingDeviceValues.shared.schedulePrimeIfNeeded()
    }
#endif
    return unityPurchasingMakeHeapAllocatedStringCopy(idfv)
}

// MARK: - ExternalPurchaseCustomLink (standalone — callback pointer per call)

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("externalPurchase_CheckEligibility")
public func externalPurchase_CheckEligibility(_ callback: @escaping ExternalPurchaseCallbackDelegateType) {
    Task.detached(priority: .userInitiated, operation: {
        await ExternalPurchaseStandalone.checkEligibility(callback: callback)
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("externalPurchase_FetchToken")
public func externalPurchase_FetchToken(_ tokenTypeCString: UnsafePointer<CChar>, _ callback: @escaping ExternalPurchaseCallbackDelegateType) {
    let tokenType = String(cString: tokenTypeCString)
    Task.detached(priority: .userInitiated, operation: {
        await ExternalPurchaseStandalone.fetchToken(tokenType: tokenType, callback: callback)
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("externalPurchase_ShowNotice")
public func externalPurchase_ShowNotice(_ noticeTypeCString: UnsafePointer<CChar>, _ callback: @escaping ExternalPurchaseCallbackDelegateType) {
    let noticeType = String(cString: noticeTypeCString)
    Task.detached(priority: .userInitiated, operation: {
        await ExternalPurchaseStandalone.showNotice(noticeType: noticeType, callback: callback)
    })
}

@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
@_cdecl("externalPurchase_FetchStorefront")
public func externalPurchase_FetchStorefront(_ callback: @escaping ExternalPurchaseCallbackDelegateType) {
    Task.detached(priority: .userInitiated, operation: {
        await ExternalPurchaseStandalone.fetchStorefront(callback: callback)
    })
}
