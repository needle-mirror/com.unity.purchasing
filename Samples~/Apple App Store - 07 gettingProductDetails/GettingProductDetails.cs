using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.AppleAppStore.GettingProductDetails
{
    [RequireComponent(typeof(UserWarningAppleAppStore))]
    public class GettingProductDetails : MonoBehaviour, IStoreListener
    {
        IStoreController m_StoreController;
        IAppleExtensions m_AppleExtensions;

        public string noAdsProductId = "com.mycompany.mygame.no_ads";

        public Text hasNoAdsText;

        void Start()
        {
            InitializePurchasing();
            UpdateWarningMessage();
        }

        void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            builder.AddProduct(noAdsProductId, ProductType.NonConsumable);

            UnityPurchasing.Initialize(this, builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("In-App Purchasing successfully initialized");

            m_StoreController = controller;
            m_AppleExtensions = extensions.GetExtension<IAppleExtensions>();

            GetProductDetailsForProductId(noAdsProductId);
        }

        public void BuyNoAds()
        {
            m_StoreController.InitiatePurchase(noAdsProductId);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var product = args.purchasedProduct;

            Debug.Log($"Processing Purchase: {product.definition.id}");
            GetProductDetailsForProductId(product.definition.id);

            return PurchaseProcessingResult.Complete;
        }

        void GetProductDetailsForProductId(string productId)
        {
            var detailsDictionary = m_AppleExtensions.GetProductDetails();
            Debug.Log($"Product Details for {productId}: {detailsDictionary[productId]}");
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

        void UpdateWarningMessage()
        {
            GetComponent<UserWarningAppleAppStore>().UpdateWarningText();
        }
    }
}
