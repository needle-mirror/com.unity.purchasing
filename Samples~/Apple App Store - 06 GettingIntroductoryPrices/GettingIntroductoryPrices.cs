using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;

namespace Samples.Purchasing.AppleAppStore.GettingIntroductoryPrices
{
    [RequireComponent(typeof(UserWarningAppleAppStore))]
    public class GettingIntroductoryPrices : MonoBehaviour, IDetailedStoreListener
    {
        IStoreController m_StoreController;
        IGooglePlayStoreExtensions m_GooglePlayStoreExtensions;
        IAppleExtensions m_AppleExtensions;

        public string subscriptionProductId = "com.mycompany.mygame.my_vip_pass_subscription";

        public Text isSubscribedText;

        void Start()
        {
            InitializePurchasing();
            UpdateWarningMessage();
        }

        void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            builder.AddProduct(subscriptionProductId, ProductType.Subscription);

            UnityPurchasing.Initialize(this, builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("In-App Purchasing successfully initialized");

            m_StoreController = controller;
            m_GooglePlayStoreExtensions = extensions.GetExtension<IGooglePlayStoreExtensions>();
            m_AppleExtensions = extensions.GetExtension<IAppleExtensions>();

            UpdateUI();
        }

        public void BuySubscription()
        {
            m_StoreController.InitiatePurchase(subscriptionProductId);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var product = args.purchasedProduct;

            Debug.Log($"Processing Purchase: {product.definition.id}");
            UpdateUI();

            return PurchaseProcessingResult.Complete;
        }

        void UpdateUI()
        {
            isSubscribedText.text = IsSubscribedTo(subscriptionProductId) ? "You are subscribed" : "You are not subscribed";
        }

        bool IsSubscribedTo(string subscriptionId)
        {
            var subscription = m_StoreController.products.WithStoreSpecificID(subscriptionId);
            if (subscription.receipt == null)
            {
                return false;
            }

            var introductorySubscriptionInfo = GetIntroductoryPriceForProductId(subscriptionId);

            var subscriptionManager = new SubscriptionManager(subscription, introductorySubscriptionInfo);
            var info = subscriptionManager.getSubscriptionInfo();

            return info.isSubscribed() == Result.True;
        }

        string GetIntroductoryPriceForProductId(string productId)
        {
            var introductoryPrices = m_AppleExtensions.GetIntroductoryPriceDictionary();
            var subscriptionIntroductionPriceInfo = introductoryPrices[productId];

            Debug.Log($"Introductory Price Information for {subscriptionProductId}: {subscriptionIntroductionPriceInfo}");

            return subscriptionIntroductionPriceInfo;
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

        void UpdateWarningMessage()
        {
            GetComponent<UserWarningAppleAppStore>().UpdateWarningText();
        }
    }
}
