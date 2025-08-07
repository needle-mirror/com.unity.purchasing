using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.Core.LocalReceiptValidation
{
    public class UserWarning : MonoBehaviour
    {
        public Text warningText;

        public void Clear()
        {
            warningText.text = "";
        }

        public void WarnInvalidStore(AppStore currentAppStore)
        {
            var warningMsg = $"Cannot validate receipts for the current store: {currentAppStore}. \n" +
                             "Build the project for Android and use the Google Play Store. See README for more information.";
            Debug.LogWarning(warningMsg);
            warningText.text = warningMsg;
        }
    }
}
