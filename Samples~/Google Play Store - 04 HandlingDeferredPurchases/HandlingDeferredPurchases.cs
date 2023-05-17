using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;

namespace Samples.Purchasing.GooglePlay.HandlingDeferredPurchases
{
    [RequireComponent(typeof(UserWarningGooglePlayStore))]
    public class HandlingDeferredPurchases : MonoBehaviour, IDetailedStoreListener
    {
        IStoreController m_StoreController;
        IGooglePlayStoreExtensions m_GooglePlayStoreExtensions;

        public string goldProductId = "com.mycompany.mygame.gold1";
        public ProductType goldType = ProductType.Consumable;

        public Text goldCountText;
        public Text waitingOnDeferredPurchaseText;

        int m_GoldCount;

        void Start()
        {
            InitializePurchasing();
            UpdateWarningMessage();
        }

        void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            builder.Configure<IGooglePlayConfiguration>().SetDeferredPurchaseListener(OnDeferredPurchase);

            builder.AddProduct(goldProductId, goldType);

            UnityPurchasing.Initialize(this, builder);
        }

        void OnDeferredPurchase(Product product)
        {
            Debug.Log($"Purchase of {product.definition.id} is deferred");
            UpdateUI();
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("In-App Purchasing successfully initialized");

            m_StoreController = controller;
            m_GooglePlayStoreExtensions = extensions.GetExtension<IGooglePlayStoreExtensions>();

            UpdateUI();
        }

        public void BuyGold()
        {
            m_StoreController.InitiatePurchase(goldProductId);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var product = args.purchasedProduct;

            Debug.Log($"Processing Purchase: {product.definition.id}");

            if (m_GooglePlayStoreExtensions.IsPurchasedProductDeferred(product))
            {
                //The purchase is Deferred.
                //Therefore, we do not unlock the content or complete the transaction.
                //ProcessPurchase will be called again once the purchase is Purchased.
                return PurchaseProcessingResult.Pending;
            }

            UnlockContent(product);

            return PurchaseProcessingResult.Complete;
        }

        void UnlockContent(Product product)
        {
            Debug.Log($"Unlock Content: {product.definition.id}");

            if (product.definition.id == goldProductId)
            {
                AddGold();
            }

            UpdateUI();
        }

        void AddGold()
        {
            m_GoldCount++;
        }

        void UpdateUI()
        {
            goldCountText.text = $"Your Gold: {m_GoldCount}";
            waitingOnDeferredPurchaseText.text = IsPurchasedProductDeferred(goldProductId) ? $"Waiting on deferred purchase: {goldProductId}" : "";
        }

        bool IsPurchasedProductDeferred(string productId)
        {
            var product = m_StoreController.products.WithID(productId);
            return m_GooglePlayStoreExtensions.IsPurchasedProductDeferred(product);
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
            GetComponent<UserWarningGooglePlayStore>().UpdateWarningText();
        }
    }
}
