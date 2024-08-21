@available(iOS 15.0, macOS 12.0, tvOS 15.0, visionOS 1.0, *)
public class DependencyInjector {
    public static func InitialiseWithCallback(_ callback: UnityPurchasingCallbackDelegateType) {
        let storeCallback = StoreKitCallback(callback: callback)
        DependencyContainer.register(storeCallback as StoreKitCallbackDelegate)

        DependencyContainer.register(TransactionObserverUseCase() as TransactionObserverUseCaseProtocol)
        DependencyContainer.register(TransactionUseCase() as TransactionUseCaseProtocol)
        DependencyContainer.register(ProductUseCase() as ProductUseCaseProtocol)
        DependencyContainer.register(PurchaseUseCase() as PurchaseUseCaseProtocol)
    }
}
