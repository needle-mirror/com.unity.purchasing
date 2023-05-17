using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Samples.Purchasing.AppleAppStore.FamilySharing
{
    [RequireComponent(typeof(UserWarningAppleAppStore))]
    public class FamilySharing : MonoBehaviour, IDetailedStoreListener
    {
        public Text subscriptionStatusText;
        public Text isFamilyShareableText;

        IStoreController m_StoreController;

        //Your products IDs. They should match the ids of your products in your store.
        public string familyShareableSubscriptionProductId = "com.mycompany.mygame.my_shared_subscription";

        void Start()
        {
            InitializePurchasing();
            UpdateWarningText();
        }

        void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            builder.Configure<IAppleConfiguration>().SetEntitlementsRevokedListener(EntitlementsRevokeListener);
            builder.AddProduct(familyShareableSubscriptionProductId, ProductType.Subscription);

            UnityPurchasing.Initialize(this, builder);
        }

        void EntitlementsRevokeListener(List<Product> revokedProducts)
        {
            LogRevokedProducts(revokedProducts);
            UpdateSubscriptionStatus();
        }

        static void LogRevokedProducts(List<Product> revokedProducts)
        {
            Debug.Log("The following products have been revoked: ");
            foreach (var revokedProduct in revokedProducts)
            {
                Debug.Log(revokedProduct.definition.id);
            }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            m_StoreController = controller;
            UpdateFamilyShareableStatus();
            UpdateSubscriptionStatus();
        }

        void UpdateFamilyShareableStatus()
        {
            var subscription = m_StoreController.products.WithID(familyShareableSubscriptionProductId);
            var isFamilyShareable = IsProductFamilyShareable(subscription);
            UpdateIsFamilyShareableText(isFamilyShareable);
        }

        bool IsProductFamilyShareable(Product product)
        {
            var appleProductMetadata = product.metadata.GetAppleProductMetadata();
            return appleProductMetadata?.isFamilyShareable ?? false;
        }

        void UpdateIsFamilyShareableText(bool isShareable)
        {
            isFamilyShareableText.text = isShareable
                ? "This subscription is family shareable."
                : "This subscription is not family shareable";
        }

        void UpdateSubscriptionStatus()
        {
            var subscription = m_StoreController.products.WithID(familyShareableSubscriptionProductId);
            var isSubscribed = IsSubscribedTo(subscription);
            UpdateSubscriptionStatusText(isSubscribed);
        }

        bool IsSubscribedTo(Product subscription)
        {
            // If the product doesn't have a receipt, then it wasn't purchased and the user is therefore not subscribed.
            if (subscription.receipt == null)
            {
                return false;
            }

            //The intro_json parameter is optional and is only used for the App Store to get introductory information.
            var subscriptionManager = new SubscriptionManager(subscription, null);

            // The SubscriptionInfo contains all of the information about the subscription.
            // Find out more: https://docs.unity3d.com/Packages/com.unity.purchasing@3.1/manual/UnityIAPSubscriptionProducts.html
            var info = subscriptionManager.getSubscriptionInfo();

            return info.isSubscribed() == Result.True;
        }

        void UpdateSubscriptionStatusText(bool isSubscribed)
        {
            subscriptionStatusText.text = isSubscribed ? "You are subscribed" : "You are not subscribed";
        }

        public void PurchaseFamilyShareableSubscription()
        {
            m_StoreController.InitiatePurchase(familyShareableSubscriptionProductId);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var product = args.purchasedProduct;

            Debug.Log($"Processing purchase - Product: '{product.definition.id}'");
            UnlockContent(product);

            return PurchaseProcessingResult.Complete;
        }

        void UnlockContent(Product product)
        {
            //Unlock content here
            UpdateSubscriptionStatus();
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
