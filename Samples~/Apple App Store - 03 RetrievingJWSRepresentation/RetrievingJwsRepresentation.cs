using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;

namespace Samples.Purchasing.AppleAppStore.RetrievingJwsRepresentation
{
    public class RetrievingJwsRepresentation : MonoBehaviour
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
        }

        async void InitializeIAP()
        {
            m_StoreName = UnityIAPServices.GetDefaultStore();
            m_PurchasingController = new PurchasingController(m_StoreName);

            m_PurchasingController.OnPurchasePending += OnPurchasePending;
            m_PurchasingController.OnPurchaseConfirmed += OnPurchaseConfirmed;

            await m_PurchasingController.Connect();

            FetchProducts();
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
            var jwsRepresentation = order.Info.Apple?.jwsRepresentation;
            Debug.Log($"JWS Representation for order: {jwsRepresentation}");
            // This is where you would send jwsRepresentation to server for validation
            // The gold would only be added if the server confirms the purchase.
            AddGold();
            m_PurchasingController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(string storeName, Order order)
        {
            if (order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id == goldProductId)
            {
                Debug.Log("Purchase confirmed for gold product");
                UpdateUI();
            }
        }

        public void BuyGold()
        {
            m_PurchasingController.PurchaseProduct(goldProductId, m_StoreName);
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
