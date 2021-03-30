Google Play
===========

Consumables
-----------

Unity IAP uses V3 of Google's Billing API, which features the concept of consumable products and explicit consumption API calls.

You should create all of your consumable products in the Google Publisher dashboard as 'Managed' products. Unity IAP will take care of consuming them when your application confirms a purchase.

Testing
-------

Before publishing your application you must test your in-app purchases on an Android device as an alpha or beta distribution.

Note that whilst your Alpha or Beta APK must be published to test your IAPs, this does not mean your App has to be publicly visible in the Google Play store.

In order to perform a complete end-to-end test of your in-app purchases, you must do so whilst signed into a device using a test account.

Please note the following:

* You must upload a signed, release version of your APK to Google Play that is published as an alpha or beta distribution.
* The version number of the APK that you upload must match the version number of the APK that you test with.
* After entering your purchase metadata into the Google Play publisher console, it may take up to 24 hours before you are able to purchase your in-app purchases using a test account.


