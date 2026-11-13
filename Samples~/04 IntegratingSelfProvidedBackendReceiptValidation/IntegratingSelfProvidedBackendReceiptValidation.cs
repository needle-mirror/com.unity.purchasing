using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.Core.IntegratingSelfProvidedBackendReceiptValidation
{
    public class IntegratingSelfProvidedBackendReceiptValidation : MonoBehaviour
    {
        PurchasingController m_PurchasingController;
        string m_StoreName;

        public string goldProductId = "com.mycompany.mygame.gold1";
        public ProductType goldType = ProductType.Consumable;

        public Text GoldCountText;
        public Text ProcessingPurchasesCountText;

        int m_GoldCount;
        int m_ProcessingPurchasesCount;

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
            m_PurchasingController.OnPurchaseDeferred += OnPurchaseDeferred;

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
                new(goldProductId, goldType),
            };

            m_PurchasingController.FetchProducts(initialProductsToFetch, m_StoreName);
        }

        public void BuyGold()
        {
            m_PurchasingController.PurchaseProduct(goldProductId, m_StoreName);
        }

        void OnPurchasePending(string storeName, PendingOrder order)
        {
            var product = GetFirstProductInOrder(order);
            if (product is null)
            {
                Debug.Log("Could not find product in order. Could not validate order.");
                return;
            }

            StartCoroutine(BackEndValidation(order));

            // We do not invoke `ConfirmPurchase` here as the purchase is not yet validated.
        }

        IEnumerator BackEndValidation(PendingOrder order)
        {
            m_ProcessingPurchasesCount++;
            UpdateUI();

            //Mock backend validation. Here you would call your own backend and wait for its response.
            //If the app is closed during this time, OnPurchasePending will be called again for the same purchase once the app is opened again.
            yield return MockServerSideValidation(order);

            m_ProcessingPurchasesCount--;
            UpdateUI();

            var product = GetFirstProductInOrder(order);
            Debug.Log($"Confirming purchase - Product: {GetIdFromProduct(product)}");

            if (product?.definition.id == goldProductId)
            {
                AddGold();
            }

            //Once we have done the validation in our backend, we confirm the purchase.
            m_PurchasingController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(string storeName, Order order)
        {
            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.Log("Could not find product in purchase confirmation.");
            }
            switch (order)
            {
                case ConfirmedOrder:
                    Debug.Log($"Order confirmed - Product: {GetIdFromProduct(product)}");
                    break;
                case FailedOrder:
                    Debug.Log($"Confirmation failed - Product: {GetIdFromProduct(product)}");
                    break;
                default:
                    Debug.Log("Unknown OnPurchaseConfirmed result.");
                    break;
            }
        }

        void OnPurchaseFailed(string storeName, FailedOrder order)
        {
            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.Log("Could not find product in failed order.");
            }

            Debug.Log($"Purchase failed - Product: '{GetIdFromProduct(product)}'," +
                      $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                      + $"Purchase Failure Details: {order.Details}");
        }

        Product GetFirstProductInOrder(Order order)
        {
            return order.CartOrdered.Items().FirstOrDefault()?.Product;
        }

        string GetIdFromProduct(Product product)
        {
            return product?.definition.id ?? "no product found";
        }

        // Calling PurchasingController.Connect without a listener on the PurchasingController.OnStoreConnected event will result in warnings.
        void OnStoreConnected(string storeName)
        {
            Debug.Log($"Store connected.");
            FetchProducts();
        }

        // Calling PurchasingController.Connect without a listener on the PurchasingController.OnStoreDisconnected event will result in warnings.
        void OnStoreDisconnected(string storeName, StoreConnectionFailureDescription description)
        {
            Debug.Log($"Store disconnected details: {description.message}");
        }

        // Calling PurchasingController.FetchProducts without listeners on PurchasingController.OnProductsFetched and PurchasingController.OnProductsFetchFailed will result in warnings.
        void OnProductsFetched(string storeName, List<Product> products)
        {
            Debug.Log($"Products fetched successfully for {products.Count} products.");
        }

        void OnProductsFetchedFailed(string storeName, ProductFetchFailed failure)
        {
            Debug.Log($"Products fetch failed for {failure.FailedFetchProducts.Count} products: {failure.FailureReason}");
        }

        // Calling PurchasingController.Purchase without listeners on PurchasingController.OnPurchaseDeferred will result in warnings.
        void OnPurchaseDeferred(string storeName, DeferredOrder order)
        {
            Debug.Log($"Purchase deferred - Product: {GetIdFromProduct(GetFirstProductInOrder(order))}");
        }

        YieldInstruction MockServerSideValidation(PendingOrder order)
        {
            const int waitSeconds = 3;
            Debug.Log($"Purchase Pending, Waiting for confirmation for {waitSeconds} seconds - Product: {GetIdFromProduct(GetFirstProductInOrder(order))}");
            return new WaitForSeconds(waitSeconds);
        }

        void AddGold()
        {
            m_GoldCount++;
            UpdateUI();
        }

        void UpdateUI()
        {
            GoldCountText.text = $"Your Gold: {m_GoldCount}";

            ProcessingPurchasesCountText.text = "";
            for (var i = 0; i < m_ProcessingPurchasesCount; i++)
            {
                ProcessingPurchasesCountText.text += "Purchase Processing...\n";
            }
        }
    }
}
