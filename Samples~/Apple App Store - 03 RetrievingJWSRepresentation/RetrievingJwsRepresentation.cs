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
        StoreController m_StoreController;

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
            m_StoreController = UnityIAPServices.StoreController();

            m_StoreController.OnPurchasePending += OnPurchasePending;
            m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;

            await m_StoreController.Connect();

            FetchProducts();
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
            var jwsRepresentation = order.Info.Apple?.jwsRepresentation;
            Debug.Log($"JWS Representation for order: {jwsRepresentation}");
            // This is where you would send jwsRepresentation to server for validation
            // The gold would only be added if the server confirms the purchase.
            AddGold();
            m_StoreController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(Order order)
        {
            if (order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id == goldProductId)
            {
                Debug.Log("Purchase confirmed for gold product");
                UpdateUI();
            }
        }

        public void BuyGold()
        {
            m_StoreController.PurchaseProduct(goldProductId);
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
