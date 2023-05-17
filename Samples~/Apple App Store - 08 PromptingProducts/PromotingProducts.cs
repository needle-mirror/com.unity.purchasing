using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Samples.Purchasing.AppleAppStore.PromotingProducts
{
    [RequireComponent(typeof(UserWarningAppleAppStore))]
    public class PromotingProducts : MonoBehaviour, IDetailedStoreListener
    {
        IStoreController m_StoreController;
        IAppleExtensions m_AppleExtensions;

        public string noAdsProductId = "com.mycompany.mygame.no_ads";

        void Start()
        {
            InitializePurchasing();
            UpdateWarningMessage();
        }

        void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            builder.AddProduct(noAdsProductId, ProductType.NonConsumable);

            builder.Configure<IAppleConfiguration>().SetApplePromotionalPurchaseInterceptorCallback(OnPromotionalPurchase);

            UnityPurchasing.Initialize(this, builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("In-App Purchasing successfully initialized");

            m_StoreController = controller;

            m_AppleExtensions = extensions.GetExtension<IAppleExtensions>();

            var noAds = controller.products.WithID(noAdsProductId);

            UpdateStorePromotionOrder(new List<Product> { noAds });
        }

        void UpdateStorePromotionOrder(List<Product> products)
        {
            Debug.Log("Setting Store Promotion Order.");

            m_AppleExtensions.SetStorePromotionOrder(products);

            m_AppleExtensions.FetchStorePromotionOrder(FetchStorePromotionOrderSuccess, FetchStorePromotionOrderFailure);
        }

        void FetchStorePromotionOrderSuccess(List<Product> products)
        {
            Debug.Log($"Current Promotion Order:");
            foreach (var product in products)
            {
                Debug.Log(product.definition.id);
            }
        }

        void FetchStorePromotionOrderFailure()
        {
            Debug.Log("Could not fetch Store Promotion Order.");
        }

        public void RevertToDefaultPromotionOrder()
        {
            UpdateStorePromotionOrder(new List<Product>());
        }

        public void BuyNoAds()
        {
            m_StoreController.InitiatePurchase(noAdsProductId);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var product = args.purchasedProduct;

            if (product.definition.type == ProductType.NonConsumable)
            {
                HidePromotedProduct(product);
            }

            Debug.Log($"Processing Purchase: {product.definition.id}");

            return PurchaseProcessingResult.Complete;
        }

        void HidePromotedProduct(Product product)
        {
            Debug.Log($"Setting Store Promotion Visibility for {product.definition.id} to hidden.");

            m_AppleExtensions.SetStorePromotionVisibility(product, AppleStorePromotionVisibility.Hide);

            m_AppleExtensions.FetchStorePromotionVisibility(product, FetchStorePromotionVisibilitySuccess, FetchStorePromotionVisibilityFailure);
        }

        void FetchStorePromotionVisibilitySuccess(string productId, AppleStorePromotionVisibility visibility)
        {
            Debug.Log($"Current Promotion Visibility for {productId}: {visibility}");
        }

        void FetchStorePromotionVisibilityFailure()
        {
            Debug.Log("Could not fetch Store Promotion Visibility.");
        }

        void OnPromotionalPurchase(Product item)
        {
            Debug.Log("Attempted promotional purchase: " + item.definition.id);

            // Promotional purchase has been detected. Handle this event by, e.g. presenting a parental gate.
            // Here, for demonstration purposes only, we will wait five seconds before continuing the purchase.
            StartCoroutine(ContinuePromotionalPurchases());
        }

        IEnumerator<WaitForSeconds> ContinuePromotionalPurchases()
        {
            Debug.Log("Continuing promotional purchases in 5 seconds");
            yield return new WaitForSeconds(5);
            Debug.Log("Continuing promotional purchases now");
            m_AppleExtensions.ContinuePromotionalPurchases(); // iOS and tvOS only; does nothing on Mac
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
