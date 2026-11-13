using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;

namespace Samples.Purchasing.AppleAppStore.HandlingDeferredPurchases
{
    public class HandlingDeferredPurchases : MonoBehaviour
    {
        PurchasingController m_PurchasingController;
        string m_StoreName;

        public string goldProductId = "com.mycompany.mygame.gold1";
        public ProductType goldType = ProductType.Consumable;

        public Text goldCountText;

        int m_GoldCount;

        void Awake()
        {
            InitializeIAP();
            SetupAskToBuy();
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
            m_PurchasingController.OnPurchaseDeferred += OnPurchaseDeferred;
            m_PurchasingController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_PurchasingController.OnPurchaseFailed += OnPurchaseFailed;

            await m_PurchasingController.Connect();

            FetchProducts();
        }


        void SetupAskToBuy()
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (m_PurchasingController.AppleStoreExtendedPurchaseService == null)
                {
                    Debug.LogError("Apple Store Extended Purchase Service is not available.");
                    return;
                }
                m_PurchasingController.AppleStoreExtendedPurchaseService.simulateAskToBuy = true;
            }
        }

        void FetchProducts()
        {
            var initialProductsToFetch = new List<ProductDefinition>
            {
                new(goldProductId, goldType)
            };

            m_PurchasingController.FetchProducts(initialProductsToFetch, m_StoreName);
        }

        void OnPurchasePending(string storeName, PendingOrder order)
        {
            UnlockContent(order);
            m_PurchasingController.ConfirmPurchase(order);
        }

        void OnPurchaseDeferred(string storeName, DeferredOrder order)
        {
            Debug.Log($"Purchase of {order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id} is deferred");
            UpdateUI();
        }

        void OnPurchaseConfirmed(string storeName, Order order)
        {
            if (order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id == goldProductId)
            {
                Debug.Log("Purchase confirmed for gold product");
            }
        }

        void OnPurchaseFailed(string storeName, FailedOrder failedOrder)
        {
            Debug.Log($"Purchase failed - Product: '{failedOrder.CartOrdered.Items().FirstOrDefault()?.Product.definition.id}'," +
                $" Purchase failure reason: {failedOrder.FailureReason}," +
                $" Purchase failure details: {failedOrder.Details}");
        }

        public void BuyGold()
        {
            m_PurchasingController.PurchaseProduct(goldProductId, m_StoreName);
        }

        void UnlockContent(PendingOrder order)
        {
            if (order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id == goldProductId)
            {
                Debug.Log($"Unlock Content: {goldProductId}");
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
        }
    }
}
