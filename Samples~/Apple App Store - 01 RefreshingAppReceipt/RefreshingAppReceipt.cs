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
        StoreController m_StoreController;

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
            m_StoreController = UnityIAPServices.StoreController();

            m_StoreController.OnPurchasePending += OnPurchasePending;
            m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_StoreController.OnPurchaseFailed += OnPurchaseFailed;
            m_StoreController.OnCheckEntitlement += OnCheckEntitlement;

            await m_StoreController.Connect();

            FetchProducts();
        }

        void OnPurchasePending(PendingOrder order)
        {
            m_StoreController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(Order order)
        {
            HasNoAds();
        }

        void OnCheckEntitlement(Entitlement entitlement)
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

            m_StoreController.FetchProducts(initialProductsToFetch);
        }

        public void Refresh()
        {
            m_StoreController.AppleStoreExtendedPurchaseService?.RefreshAppReceipt(OnRefreshSuccess, OnRefreshFailure);
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
            m_StoreController.PurchaseProduct(noAdsProductId);
        }

        void UpdateUI()
        {
            hasNoAdsText.text = hasNoAds ? "No ads will be shown" : "Ads will be shown";
        }
        void HasNoAds()
        {
            m_StoreController.CheckEntitlement(m_StoreController.GetProducts().FirstOrDefault(product => product.definition.id == noAdsProductId));
        }

        void OnPurchaseFailed(FailedOrder failedOrder)
        {
            Debug.Log($"Purchase failed - Product: '{failedOrder.CartOrdered.Items().FirstOrDefault()?.Product.definition.id}'," +
                $" Purchase failure reason: {failedOrder.FailureReason}," +
                $" Purchase failure details: {failedOrder.Details}");
            HasNoAds();
        }
    }
}
