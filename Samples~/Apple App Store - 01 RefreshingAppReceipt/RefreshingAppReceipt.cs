using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;

namespace Samples.Purchasing.AppleAppStore.RefreshingAppReceipt
{
    public class RefreshingAppReceipt : MonoBehaviour
    {
        PurchasingController m_PurchasingController;
        string m_StoreName;

        public string noAdsProductId = "com.mycompany.mygame.no_ads";

        public Text refreshReceiptText;
        public Text hasNoAdsText;
        public bool hasNoAds;

        void Awake()
        {
            InitializeIAP();
        }

        void Start()
        {
            UpdateUI();
        }

        async void InitializeIAP()
        {
            m_StoreName = UnityIAPServices.GetDefaultStore();
            m_PurchasingController = new PurchasingController(m_StoreName);

            m_PurchasingController.OnPurchasePending += OnPurchasePending;
            m_PurchasingController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_PurchasingController.OnPurchaseFailed += OnPurchaseFailed;
            m_PurchasingController.OnCheckEntitlement += OnCheckEntitlement;

            await m_PurchasingController.Connect();

            FetchProducts();
        }

        void OnPurchasePending(string storeName, PendingOrder order)
        {
            m_PurchasingController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(string storeName, Order order)
        {
            HasNoAds();
        }

        void OnCheckEntitlement(string storeName, Entitlement entitlement)
        {
            if (entitlement.Product.definition.id == noAdsProductId)
            {
                switch (entitlement.Status)
                {
                    case EntitlementStatus.FullyEntitled:
                        hasNoAds = true;
                        UpdateUI();
                        break;
                    default:
                        hasNoAds = false;
                        UpdateUI();
                        break;
                }
            }
        }

        void FetchProducts()
        {
            var initialProductsToFetch = new List<ProductDefinition>
            {
                new ProductDefinition(noAdsProductId, ProductType.NonConsumable)
            };

            m_PurchasingController.FetchProducts(initialProductsToFetch, m_StoreName);
        }

        public void Refresh()
        {
            m_PurchasingController.AppleStoreExtendedPurchaseService?.RefreshAppReceipt(OnRefreshSuccess, OnRefreshFailure);
        }

        void OnRefreshSuccess(string receipt)
        {
            // This does not mean anything was modified,
            // merely that the refresh process succeeded.
            // For information on parsing receipts, see:
            // https://developer.apple.com/library/archive/releasenotes/General/ValidateAppStoreReceipt/Chapters/ValidateLocally.html#//apple_ref/doc/uid/TP40010573-CH1-SW2
            // as well as:
            // https://docs.unity3d.com/Manual/UnityIAPValidatingReceipts.html
            var message = $"Refresh Successful: {receipt}";
            Debug.Log(message);
            refreshReceiptText.text = message;
        }

        void OnRefreshFailure(string error)
        {
            var message = $"Refresh Failed: {error}";
            Debug.Log(message);
            refreshReceiptText.text = message;
        }

        public void BuyNoAds()
        {
            m_PurchasingController.PurchaseProduct(noAdsProductId, m_StoreName);
        }

        void UpdateUI()
        {
            hasNoAdsText.text = hasNoAds ? "No ads will be shown" : "Ads will be shown";
        }
        void HasNoAds()
        {
            m_PurchasingController.CheckEntitlement(m_PurchasingController.GetProducts(m_StoreName).FirstOrDefault(product => product.definition.id == noAdsProductId));
        }

        void OnPurchaseFailed(string storeName, FailedOrder failedOrder)
        {
            Debug.Log($"Purchase failed - Product: '{failedOrder.CartOrdered.Items().FirstOrDefault()?.Product.definition.id}'," +
                $" Purchase failure reason: {failedOrder.FailureReason}," +
                $" Purchase failure details: {failedOrder.Details}");
            HasNoAds();
        }
    }
}
