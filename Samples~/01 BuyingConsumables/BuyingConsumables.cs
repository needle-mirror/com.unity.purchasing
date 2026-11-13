using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.Legacy.Core.BuyingConsumables
{
    public class BuyingConsumables : MonoBehaviour
    {
        PurchasingController m_PurchasingController; // The Unity Purchasing system.
        string m_StoreName; // The store this sample buys from.

        //Your products IDs. They should match the ids of your products in your store.
        public string goldProductId = "com.mycompany.mygame.gold1";
        public string diamondProductId = "com.mycompany.mygame.diamond1";

        public Text GoldCountText;
        public Text DiamondCountText;

        int m_GoldCount;
        int m_DiamondCount;

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

            m_PurchasingController.OnStoreConnected += OnStoreConnected;
            m_PurchasingController.OnStoreDisconnected += OnStoreDisconnected;

            m_PurchasingController.OnProductsFetchFailed += OnProductsFetchedFailed;
            m_PurchasingController.OnProductsFetched += OnProductsFetched;

            Debug.Log("Connecting to store.");
            await m_PurchasingController.Connect();
        }

        void FetchProducts()
        {
            var initialProductsToFetch = new List<ProductDefinition>
            {
                new(goldProductId, ProductType.Consumable),
                new(diamondProductId, ProductType.Consumable)
            };

            m_PurchasingController.FetchProducts(initialProductsToFetch, m_StoreName);
        }

        public void BuyGold()
        {
            m_PurchasingController.PurchaseProduct(goldProductId, m_StoreName);
        }

        public void BuyDiamond()
        {
            m_PurchasingController.PurchaseProduct(diamondProductId, m_StoreName);
        }

        void OnPurchaseFailed(string storeName, FailedOrder order)
        {
            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.Log("Could not find product in failed order.");
            }

            Debug.Log($"Purchase failed - Product: '{product?.definition.id}'," +
                      $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                      + $"Purchase Failure Details: {order.Details}");
        }

        void OnPurchasePending(string storeName, PendingOrder order)
        {
            var product = GetFirstProductInOrder(order);
            if (product is null)
            {
                Debug.Log("Could not find product in order.");
                return;
            }

            //Add the purchased product to the players inventory
            if (product.definition.id == goldProductId)
            {
                AddGold();
            }
            else if (product.definition.id == diamondProductId)
            {
                AddDiamond();
            }

            Debug.Log($"Purchase complete - Product: {product.definition.id}");

            m_PurchasingController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(string storeName, Order order)
        {
            switch (order)
            {
                case ConfirmedOrder confirmedOrder:
                    OnPurchaseConfirmed(confirmedOrder);
                    break;
                case FailedOrder failedOrder:
                    OnPurchaseConfirmationFailed(failedOrder);
                    break;
                default:
                    Debug.Log("Unknown OnPurchaseConfirmed result.");
                    break;
            }
        }

        void OnPurchaseConfirmed(ConfirmedOrder order)
        {
            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.Log("Could not find product in purchase confirmation.");
            }

            Debug.Log($"Purchase confirmed- Product: {product?.definition.id}");
        }

        void OnPurchaseConfirmationFailed(FailedOrder order)
        {
            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.Log("Could not find product in failed confirmation.");
            }

            Debug.Log($"Confirmation failed - Product: '{product?.definition.id}'," +
                      $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                      + $"Confirmation Failure Details: {order.Details}");
        }

        Product GetFirstProductInOrder(Order order)
        {
            return order.CartOrdered.Items().First()?.Product;
        }

        // Calling PurchasingController.Connect without a listener on the PurchasingController.OnStoreConnected event will result in warnings.
        void OnStoreConnected(string storeName)
        {
            Debug.Log($"Store Connected.");
            FetchProducts();
        }

        // Calling PurchasingController.Connect without a listener on the PurchasingController.OnStoreDisconnected event will result in warnings.
        void OnStoreDisconnected(string storeName, StoreConnectionFailureDescription description)
        {
            Debug.Log($"Store disconnected details: {description.message}");
        }

        // Calling PurchasingController.Connect without listeners on PurchasingController.OnProductsFetched and PurchasingController.OnProductsFetchFailed will result in warnings.
        void OnProductsFetched(string storeName, List<Product> products)
        {
            Debug.Log($"Products fetched successfully for {products.Count} products.");
        }

        void OnProductsFetchedFailed(string storeName, ProductFetchFailed failure)
        {
            Debug.Log($"Products fetch failed for {failure.FailedFetchProducts.Count} products: {failure.FailureReason}");
        }

        void AddGold()
        {
            m_GoldCount++;
            UpdateUI();
        }

        void AddDiamond()
        {
            m_DiamondCount++;
            UpdateUI();
        }

        void UpdateUI()
        {
            GoldCountText.text = $"Your Gold: {m_GoldCount}";
            DiamondCountText.text = $"Your Diamonds: {m_DiamondCount}";
        }
    }
}
