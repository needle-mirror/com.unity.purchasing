## README - In-App Purchasing Sample Scene

This sample showcases how to set up your Unity In-App Purchasing paywall.

By default, it sells through Payment Providers, with products from the Remote Catalog of your linked Unity Cloud project. This needs the `com.unity.services.authentication` package (3.8.1 or later). To sell through the platform store (Apple App Store or Google Play) from a catalog defined in code instead, remove the `#define usePaymentProviderOnly` line at the top of `PaywallManager.cs`. The sample then needs only the In-App Purchasing package.

## Instructions to test this sample:

Fake Store (Play mode in editor):
No setup required, but functionalities are limited.

Google Play Store:
1. Link your project in `Project Settings` > `Services`
2. Have in-app purchasing correctly configured with
   the [Google Play Store](https://docs.unity.com/en-us/iap/google-store)
3. [Add your Google Play license key](https://docs.unity.com/en-us/iap/add-a-google-license-key-to-your-project)
4. [Obfuscate the license key](https://docs.unity.com/en-us/iap/add-a-google-license-key-to-your-project) to generate the `GooglePlayTangle` class. The sample uses it to validate receipts on the device, and needs it to build for Android

Apple App Store:
1. Link your project in `Project Settings` > `Services`
2. Have In-App Purchasing correctly configured with the [Apple App Store](https://docs.unity.com/en-us/iap/apple-store)

Payment Providers (default):
1. Install the `com.unity.services.authentication` package (3.8.1 or later). Payment Providers need a signed-in player.
2. Link your project in `Project Settings` > `Services`. Products are fetched from the Remote Catalog of that Unity Cloud project.

The sample signs the player in anonymously to stay short. Anonymous sign-in does not persist, so use a persistent identity in your game.

For more information about the In-App Purchasing package, see the [IAP manual](https://docs.unity.com/ugs/en-us/manual/iap/manual/overview).
