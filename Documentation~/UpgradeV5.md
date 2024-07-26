## Upgrading to Version 5 of IAP
*This section is only if you already had In-App Purchasing package installed prior to version 5.*

Most of the previous IAP should still be accessible but are marked as deprecated. They are also calling the new code so functionalities may vary.

### Codeless IAP ###
If your project is using Codeless IAP button and listeners, everything will be upgraded automatically. Your project will be using v5 without having to do anything.


### Initializing IAP ###
Previously initializing IAP would connect to the store, fetch products and purchases. When everything was finished it would return that the init was successful. If any of those calls failed during that process it would fail initialization.

Now for v5, we no longer require initializing. Connecting to the store, fetching products and fetching purchasing can all be done at will and asynchronously. To learn more see pages under the [Set up and integrating Unity IAP](Overview.md) section

### Extension / Configuration ###
Previously extensions and configuration for stores were a separate flow and classes.

Now they are directly part of the our 3 main services ProductService, PurchaseService and StoreService. Simply call `UnityIAPServices.Product.Google` as an example to see the list of functionalities only available to google under or product service. This works the same for all 3 services and all stores.

### Custom Stores ###
Coming soon...

### Deprecated ###
- `IAPManager` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPListener` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPButton` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPProduct` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPProductDefinition` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStore` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreExtension` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreListener` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreProduct` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreProductDefinition` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreService` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreServiceListener` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreServiceProduct` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreServiceProductDefinition` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreServiceProductListener` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreServiceStore` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreServiceStoreListener` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreServiceStoreProduct` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreServiceStoreProductDefinition` class is now deprecated. Use `UnityIAPServices` instead.
- `IAPStoreServiceStoreProductListener` class is now deprecated. Use `UnityIAPServices` instead.
