## README - In-App Purchasing Sample Scenes - App Store - Can Make Payments

This sample shows how to check whether the logged-in player is permitted to purchase from the Apple App Store on this device.
This allows developer to know if they may need to alter its behavior or appearance before it can engage the player with in-app
purchasing.


## Instructions to test this sample:

1. Have in-app purchasing correctly configured with
   the [Apple App Store](https://docs.unity3d.com/Packages/com.unity.purchasing@3.2/manual/UnityIAPAppleConfiguration.html).
2. Build your project for `iOS`.
3. To test, click `Can Player Make Payments` and see the text output for result. NOTE: On non-Apple platforms this returns **True**.
4. To test the negative case, where the player is *restricted* from making payments:
    1. Enable **Content & Privacy Restrictions** in the **Screen Time** section of your iOS device's **Settings**.
    2. Choose **"Don't Allow"** for **In-app purchases** from the **iTunes & App Store Purchases** restriction setting.
    3. Repeat this test.

## Can Make Payments

See Apple's [canMakePayments documentation](https://developer.apple.com/documentation/storekit/appstore/3822277-canmakepayments/) for more information.
