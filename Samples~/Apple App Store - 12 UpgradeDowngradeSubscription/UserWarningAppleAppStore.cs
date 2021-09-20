using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.AppleAppStore.UpgradeDowngradeSubscription
{
    public class UserWarningAppleAppStore : MonoBehaviour
    {
        public Text warningText;

        public void UpdateWarningText()
        {
            var currentAppStore = StandardPurchasingModule.Instance().appStore;

            var warningMessage = currentAppStore != AppStore.AppleAppStore
                && currentAppStore != AppStore.MacAppStore ?
                "This sample is meant to be tested using the Apple App Store.\n" +
                $"The currently selected store is: {currentAppStore}.\n" +
                "Build the project for iOS, tvOS, or macOS.\n\n" +
                "See README for more information and instructions on how to test this sample."
                : "";

            warningText.text = warningMessage;
        }
    }
}
