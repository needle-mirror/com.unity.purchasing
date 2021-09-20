using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.AppleAppStore.CanMakePayments
{
    [RequireComponent(typeof(UserWarningAppleAppStore))]
    public class CanMakePayments : MonoBehaviour
    {
        ConfigurationBuilder m_ConfigurationBuilder;
        IAppleConfiguration AppleConfiguration => m_ConfigurationBuilder.Configure<IAppleConfiguration>();

        public Text canMakePaymentsText;

        void Start()
        {
            Initialize();
            UpdateWarningMessage();
        }

        void Initialize()
        {
            m_ConfigurationBuilder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            Debug.Log("In-App Purchasing started configuring successfully");
        }

        public void AskCanMakePayments()
        {
            UpdateUI();
        }

        void UpdateUI()
        {
            canMakePaymentsText.text = "Can Make Payments: " + AppleCanMakePayments();
        }

        bool AppleCanMakePayments()
        {
            return AppleConfiguration.canMakePayments;
        }

        void UpdateWarningMessage()
        {
            GetComponent<UserWarningAppleAppStore>().UpdateWarningText();
        }
    }
}
