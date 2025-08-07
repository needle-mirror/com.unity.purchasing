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
        StoreController m_StoreController;

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
            m_StoreController = UnityIAPServices.StoreController();

            m_StoreController.OnPurchasePending += OnPurchasePending;
            m_StoreController.OnPurchaseDeferred += OnPurchaseDeferred;
            m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_StoreController.OnPurchaseFailed += OnPurchaseFailed;

            await m_StoreController.Connect();

            FetchProducts();
        }


        void SetupAskToBuy()
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (m_StoreController.AppleStoreExtendedPurchaseService == null)
                {
                    Debug.LogError("Apple Store Extended Purchase Service is not available.");
                    return;
                }
                m_StoreController.AppleStoreExtendedPurchaseService.simulateAskToBuy = true;
            }
        }

        void FetchProducts()
        {
            var initialProductsToFetch = new List<ProductDefinition>
            {
                new(goldProductId, goldType)
            };

            m_StoreController.FetchProducts(initialProductsToFetch);
        }

        void OnPurchasePending(PendingOrder order)
        {
            UnlockContent(order);
            m_StoreController.ConfirmPurchase(order);
        }

        void OnPurchaseDeferred(DeferredOrder order)
        {
            Debug.Log($"Purchase of {order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id} is deferred");
            UpdateUI();
        }

        void OnPurchaseConfirmed(Order order)
        {
            if (order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id == goldProductId)
            {
                Debug.Log("Purchase confirmed for gold product");
            }
        }

        void OnPurchaseFailed(FailedOrder failedOrder)
        {
            Debug.Log($"Purchase failed - Product: '{failedOrder.CartOrdered.Items().FirstOrDefault()?.Product.definition.id}'," +
                $" Purchase failure reason: {failedOrder.FailureReason}," +
                $" Purchase failure details: {failedOrder.Details}");
        }

        public void BuyGold()
        {
            m_StoreController.PurchaseProduct(goldProductId);
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
