using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;

namespace Samples.Purchasing.AppleAppStore.UpgradeDowngradeSubscription
{
    [RequireComponent(typeof(UserWarningAppleAppStore))]
    public class UpgradeDowngradeSubscription : MonoBehaviour, IDetailedStoreListener
    {
        //Your products IDs. They should match the ids of your products in your store.
        public string normalSubscriptionId = "com.mycompany.mygame.my_normal_pass_subscription";
        public string vipSubscriptionId = "com.mycompany.mygame.my_vip_pass_subscription";

        public Text currentSubscriptionText;
        public Text deferredSubscriptionChangeText;

        SubscriptionGroup m_SubscriptionGroup;

        void Start()
        {
            InitializePurchasing();
            UpdateWarningText();
        }

        void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            builder.AddProduct(normalSubscriptionId, ProductType.Subscription);
            builder.AddProduct(vipSubscriptionId, ProductType.Subscription);

            UnityPurchasing.Initialize(this, builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("In-App Purchasing successfully initialized");

            // Define the subscription group here, duplicating the subscription group on App Store Connect
            m_SubscriptionGroup = new SubscriptionGroup(controller, extensions,
                normalSubscriptionId, vipSubscriptionId);
            UpdateUI(m_SubscriptionGroup.CurrentSubscriptionId());
        }

        public void BuyNormalSubscription()
        {
            m_SubscriptionGroup.BuySubscription(normalSubscriptionId);
        }

        public void BuyVipSubscription()
        {
            m_SubscriptionGroup.BuySubscription(vipSubscriptionId);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var product = args.purchasedProduct;

            Debug.Log($"Processing Purchase: {product.definition.id}");
            UnlockContent(product);

            return PurchaseProcessingResult.Complete;
        }

        void UnlockContent(Product product)
        {
            //Unlock content here
            UpdateUI(product.definition.id);
        }

        void UpdateUI(string subscriptionId)
        {
            currentSubscriptionText.text = $"Current Subscription: {subscriptionId ?? "None"}";
            deferredSubscriptionChangeText.text = "";
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, null);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            var errorMessage = $"Purchasing failed to initialize. Reason: {error}.";

            if (message != null)
            {
                errorMessage += $" More details: {message}";
            }

            Debug.Log(errorMessage);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.Log($"Purchase failed - Product: '{product.definition.id}', PurchaseFailureReason: {failureReason}");
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.Log($"Purchase failed - Product: '{product.definition.id}'," +
                $" Purchase failure reason: {failureDescription.reason}," +
                $" Purchase failure details: {failureDescription.message}");
        }

        void UpdateWarningText()
        {
            GetComponent<UserWarningAppleAppStore>().UpdateWarningText();
        }
    }
}
