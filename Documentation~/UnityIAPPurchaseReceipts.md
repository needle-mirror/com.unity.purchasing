Purchase Receipts
=================

Unity IAP provides purchase receipts as a JSON hash containing the following keys and values:

|Key|Value|
|:---|:---|
|__Store__|The name of the store in use, such as **GooglePlay** or **AppleAppStore**|
|__TransactionID__|This transaction’s unique identifier, provided by the store|
|__Payload__|Varies by platform, details below.|

iOS
---

Payload varies depending upon the device's iOS version.

|iOS version|Payload|
|:---|:---|
|__iOS &gt;= 7__|payload is a base 64 encoded [App Receipt](https://developer.apple.com/library/ios/releasenotes/General/ValidateAppStoreReceipt/Chapters/ReceiptFields.html#/apple_ref/doc/uid/TP40010573-CH106-SW1).|
|__iOS &lt; 7__|payload is a [SKPaymentTransaction transactionReceipt](https://developer.apple.com/library/ios/documentation/StoreKit/Reference/SKPaymentTransaction_Class/).|

Mac App Store
-------------

Payload is a base 64 encoded [App Receipt](https://developer.apple.com/library/ios/releasenotes/General/ValidateAppStoreReceipt/Chapters/ReceiptFields.html#/apple_ref/doc/uid/TP40010573-CH106-SW1).

Google Play
-----------

Payload is a JSON hash with the following keys and values:

|Key|Value|
|:---|:---|
|__json__|A JSON encoded string provided by Google; [`INAPP_PURCHASE_DATA`](http://developer.android.com/google/play/billing/billing_reference.html)|
|__signature__|A signature for the json parameter, as provided by Google; [`INAPP_DATA_SIGNATURE`](http://developer.android.com/google/play/billing/billing_reference.html)|

Universal Windows Platform
-------------

Payload is an XML string as [specified by Microsoft](https://msdn.microsoft.com/en-US/library/windows/apps/windows.applicationmodel.store.currentapp.getappreceiptasync.aspx)


