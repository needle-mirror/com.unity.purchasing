import Foundation

@available(iOS 13.0.0, *)
public protocol StoreKitCallbackDelegate {
    func callback(subject: String, payload: String, entitlementStatus: Int) async
    func callbackPtr(subject: String, payload: String, entitlementStatus: Int) async
}

@available(iOS 13.0.0, *)
public class StoreKitCallback: StoreKitCallbackDelegate {
    var storeKitCallbackDelegate: UnityPurchasingCallbackDelegateType? = nil

    public init(callback: UnityPurchasingCallbackDelegateType) {
        storeKitCallbackDelegate = callback
    }

    public func callback(subject: String, payload: String, entitlementStatus: Int) async {
        await MainActor.run {
            let subjectCStr = subject.toCString()
            let payloadCStr = payload.toCString()

            storeKitCallbackDelegate!(subjectCStr, payloadCStr, entitlementStatus, nil)
        }
    }

    public func callbackPtr(subject: String, payload: String, entitlementStatus: Int) async {
        await MainActor.run {
            let subjectCStr = subject.toCString()
            let stringPtr = unityPurchasingMakeHeapAllocatedStringCopy(payload)

            storeKitCallbackDelegate!(subjectCStr, "", entitlementStatus, stringPtr)
        }
    }
}
