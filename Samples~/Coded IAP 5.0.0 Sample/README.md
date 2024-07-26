## README - In-App Purchasing Sample Scene

This sample showcases how to setup your Unity In-App Purchasing paywall.

## Instructions to test this sample:

Fake Store (Play mode in editor):
No setup required, but functionalities are limited.

Google Play Store:
1. Link your project in `Project Settings` > `Services`
2. Have in-app purchasing correctly configured with
   the [Google Play Store](https://docs.unity3d.com/Packages/com.unity.purchasing@4.12/manual/UnityIAPGoogleConfiguration.html)
3. [Set your Google Public Key](https://docs.unity3d.com/Packages/com.unity.purchasing@4.12/manual/GooglePublicKey.html)
4. [Obfuscate the key](https://docs.unity3d.com/Packages/com.unity.purchasing@4.12/manual/UnityIAPValidatingReceipts.html)

Apple App Store:
1. Link your project in `Project Settings` > `Services`
2. Have in-app purchasing correctly configured with the [Apple App Store](https://docs.unity3d.com/Packages/com.unity.purchasing@4.12/manual/UnityIAPAppleConfiguration.html)
3. [Obfuscate the key](https://docs.unity3d.com/Packages/com.unity.purchasing@4.12/manual/UnityIAPValidatingReceipts.html)

###### *You can change the currently selected store under `Services > In-App Purchasing > Configure` and changing the `Current Targeted Store` field.
