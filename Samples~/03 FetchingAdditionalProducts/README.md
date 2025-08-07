# In-App Purchasing Sample Scenes - Fetching Additional Products

This sample demonstrates how to use the `StoreController` to dynamically fetch
and display additional products that can be purchased in your store.

The sample uses a fake store for transactions.
To use a real store (such as the App Store or Google Play Store), register your application and configure In-App Purchases.

Product identifiers are defined in the `FetchingAdditionalProducts.cs` file.

## Fetch Additional Products

Fetching additional products allows you to add new in-app purchasable items dynamically after initialization.

**Examples:**
- Add new products without updating the app on the store.
- Manage products from a content management system and fetch them remotely.

This sample also demonstrates proper event subscription and unsubscription to avoid duplicate event handling.

For more information about the In-App Purchasing package, see the [IAP manual](https://docs.unity.com/ugs/en-us/manual/iap/manual/overview).
