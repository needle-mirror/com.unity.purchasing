# Samsung Galaxy IAP configuration

This guide describes the process of establishing the digital records and relationships necessary for a Unity app to interact with an in-app purchase store. The [Unity IAP](UnityIAP) purchasing API is targeted.

In-app purchasing (IAP) is the process of transacting money for digital goods. A platform’s store allows the purchase of products, representing digital goods. These products have an identifier, typically of string datatype. Products have types to represent their durability: the most common are subscription (capable of being subscribed to), consumable (capable of being rebought), and non-consumable (capable of being bought once).

**Note**: The Samsung Galaxy store only apply to version 3.1.0 and earlier of the Unity In App Purchasing package and is now deprecated. Please use the [Unity Distribution Platform](https://docs.unity3d.com/2021.2/Documentation/Manual/udp.html) instead.

### Cross-store implementation of in-app purchases

Note that there are cross-store installation issues when using shared Android bundle identifiers to publish to multiple Android in-app purchase stores (such as Samsung and Google) simultaneously.  See documentation on [Cross-store installation issues with Android in-app purchasing](UnityIAPCrossStoreInstallationIssues) for more information.

## Samsung Galaxy apps

### Getting started

1. Write an app implementing Unity IAP. See [Unity IAP initialization](UnityIAPInitialization) and [Integrating Unity IAP with your app](https://unity3d.com/learn/tutorials/topics/analytics/integrating-unity-iap-your-game).

2. Keep the app’s product identifiers on-hand for use with the [Samsung Apps Seller Office](http://seller.samsungapps.com/) later. 

![](../uploads/Main/SamsungGalaxyIAP-0.png)

3. To set the IAP target store in the Unity Editor, go to __Window__ > __Unity IAP__ > __Android__ > __Target Samsung Galaxy Apps__.

![](../uploads/Main/SamsungGalaxyIAP-1.jpg)

Alternatively, call the Editor API:

`UnityPurchasingEditor.TargetAndroidStore(AndroidStore.SamsungApps`

4. Build a signed non-Development Build Android APK from your app. See Unity's [Android](android) documentation to learn more. 

**Tip**: Take special precautions to safely store your keystore file. The original keystore is always required to update a published application. 

### Register the application

Register the Android application with the [Samsung Galaxy Apps Seller Office](http://seller.samsungapps.com/).

1. Choose __Add New Application__.

![](../uploads/Main/SamsungGalaxyIAP-2.png)

2. Choose the __Android__ option and select a __Default Language__.

![](../uploads/Main/SamsungGalaxyIAP-3.jpg)

3. To enable in-app purchasing, first register a binary APK. Go to __In App Purchase__ and click __GO__.

![](../uploads/Main/SamsungGalaxyIAP-4.png)

In the App Store Developer Console, go to __Binary__ and select __Add binary__. 

![](../uploads/Main/SamsungGalaxyIAP-5.png)

Populate the device characteristics in __Resolution(s)__ and __Google Mobile Service__, upload your APK (the one you created above in the "Getting Started" section) in __Binary upload__, then click __Save__.

![](../uploads/Main/SamsungGalaxyIAP-6.png)

Wait for the APK upload to complete, then click __Save__.

![](../uploads/Main/SamsungGalaxyIAP-7.png)

### Add in-app purchases

In the Seller Office, add one or more in-app purchases for the app.

1. Go to __In App Purchase__ and choose __Add Item__.

![](../uploads/Main/SamsungGalaxyIAP-8.png)

2. Define the __Item ID__. The Item ID here is the same identifier used in the app source code, added to the Unity IAP [ConfigurationBuilder](ScriptRef:Purchasing.ConfigurationBuilder.html) instance via `AddProduct()` or `AddProducts()`. For debugging purposes, it's best practise to use [reverse-DNS](https://en.wikipedia.org/wiki/Reverse_domain_name_notation) for your Item ID. Click __Check__ to ensure the Item ID is valid and unique, then populate __Item Type__ and all other elements and click __Save__.

![](../uploads/Main/SamsungGalaxyIAP-9.png)

3. View the result in __In App Purchase__:

![](../uploads/Main/SamsungGalaxyIAP-10.png)

### Testing an IAP implementation

The Samsung Galaxy App Store supports testing via the __Developer mode__ value in the app before making purchases. This special build of the app connects with Samsung’s billing servers and performs fake purchases. This does not incur real-world monetary costs related to the product, and allows you to test the app’s purchasing logic.

1. Modify the app’s Unity IAP integration, adding the following line after creating the `ConfigurationBuilder` instance:
`builder.Configure<ISamsungAppsConfiguration>().SetMode(SamsungAppsMode.AlwaysSucceed); // TESTING: auto-approves all transactions by Samsung`.
You can also configure this to fail all transactions via the `SamsungAppsMode.AlwaysFail` enumeration, enabling you to test all your error code.

2. Build and run the app, testing its in-app purchasing logic. As long as developer mode is implemented, this does not incur real-world monetary costs.

![](../uploads/Main/SamsungGalaxyIAP-11.png)

3. **IMPORTANT**: When testing is complete, make sure you remove the `SetMode` line. This ensures users pay real-world money when tha app is in use.



<!-- area:monetization -->

