using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;

namespace Samples.Purchasing.AppleAppStore.PresentCodeRedemptionSheet
{
    [RequireComponent(typeof(UserWarningAppleAppStore))]
    public class PresentCodeRedemptionSheet : MonoBehaviour, IDetailedStoreListener
    {
        IStoreController m_StoreController;
        IAppleExtensions m_AppleExtensions;

        public string normalSubscriptionId = "com.mycompany.mygame.my_normal_pass_subscription";

        public Text ownsSubscription;

        void Start()
        {
            InitializePurchasing();
            UpdateWarningMessage();
        }

        void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            builder.AddProduct(normalSubscriptionId, ProductType.Subscription);

            UnityPurchasing.Initialize(this, builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("In-App Purchasing successfully initialized");

            m_StoreController = controller;
            m_AppleExtensions = extensions.GetExtension<IAppleExtensions>();

            UpdateUI();
        }

        public void DoPresentCodeRedemptionSheet()
        {
            Debug.Log("$Calling Apple API to present code redemption sheet ...");
            Debug.LogWarning($"Next, user should input the previously generated Offer Code from App Store Connect, for Product ID: {normalSubscriptionId}");
            Debug.Log("After, Apple StoreKit should generate a purchase, and trigger a ProcessPurchase callback for it.");
            Debug.Log("See README.md for more information.");
            m_AppleExtensions.PresentCodeRedemptionSheet();
        }

        public void BuyNormalSubscription_DoNotCallForThisSample()
        {
            // Ownership of this product for the purposes of this Sample MUST happen, indirectly,
            // with the Apple "Code Redemption Sheet"
            m_StoreController.InitiatePurchase(normalSubscriptionId);
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
            ownsSubscription.text = HasNormalSubscription() ? "Subscription is owned" : "Subscription is not yet owned";
        }

        bool HasNormalSubscription()
        {
            var normalSubscriptionProduct = m_StoreController.products.WithID(normalSubscriptionId);
            return normalSubscriptionProduct != null && normalSubscriptionProduct.hasReceipt;
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
