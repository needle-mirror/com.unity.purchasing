using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.Core.FetchingAdditionalProducts
{
    public class FetchingAdditionalProducts : MonoBehaviour
    {
        PurchasingController m_PurchasingController;
        string m_StoreName;

        public string goldProductId = "com.mycompany.mygame.gold1";
        public ProductType goldType = ProductType.Consumable;

        //This product will only be fetched once the FetchAdditionalProducts button is clicked
        public string diamondProductId = "com.mycompany.mygame.diamond1";
        public ProductType diamondType = ProductType.Consumable;

        public Text GoldCountText;
        public Text DiamondCountText;

        public GameObject additionalProductsPanel;

        int m_GoldCount;
        int m_DiamondCount;

        void Awake()
        {
            additionalProductsPanel.SetActive(false); // Hide diamond UI at start, as user must fetch them first
            InitializeIAP();
            UpdateUI();
        }

        async void InitializeIAP()
        {
            m_StoreName = UnityIAPServices.GetDefaultStore();
            m_PurchasingController = new PurchasingController(m_StoreName);
            m_PurchasingController.OnPurchasePending += OnPurchasePending;
            m_PurchasingController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_PurchasingController.OnStoreConnected += OnStoreConnected;
            m_PurchasingController.OnStoreDisconnected += OnStoreDisconnected;

            await m_PurchasingController.Connect();
        }

        public void OnFetchAdditionalProductsButtonClicked()
        {
            FetchAdditionalProducts();
        }

        void FetchAdditionalProducts()
        {
            var additionalProductsToFetch = new List<ProductDefinition>
            {
                new ProductDefinition(diamondProductId, diamondType)
            };

            Debug.Log($"Fetching additional products in progress");
            m_PurchasingController.OnProductsFetched += OnProductsFetched;
            m_PurchasingController.OnProductsFetchFailed += OnProductsFetchFailed;
            m_PurchasingController.FetchProducts(additionalProductsToFetch, m_StoreName);
        }

        void OnPurchasePending(string storeName, PendingOrder order)
        {
            Debug.Log($"Purchase pending - Order: {order}");
            m_PurchasingController.ConfirmPurchase(order);
            // Optionally, update UI to show pending state
        }

        void OnPurchaseConfirmed(string storeName, Order order)
        {
            var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
            if (product == null)
            {
                Debug.LogError("Purchase confirmed but product is null.");
                return;
            }

            Debug.Log($"Purchase confirmed - Product: {product.definition.id}");
            if (product.definition.id == goldProductId)
            {
                AddGold();
            }
            else if (product.definition.id == diamondProductId)
            {
                AddDiamond();
            }
            // Optionally, update UI to show purchase success
        }

        public void BuyGold()
        {
            m_PurchasingController.PurchaseProduct(goldProductId, m_StoreName);
        }

        public void BuyDiamond()
        {
            m_PurchasingController.PurchaseProduct(diamondProductId, m_StoreName);
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

        void OnStoreConnected(string storeName)
        {
            Debug.Log($"Store connected.");
        }

        void OnStoreDisconnected(string storeName, StoreConnectionFailureDescription storeConnectionFailureDescription)
        {
            Debug.Log($"Store disconnected. Reason: {storeConnectionFailureDescription}");
            // Optionally, update UI
        }

        void OnProductsFetchFailed(string storeName, ProductFetchFailed productFetchFailed)
        {
            m_PurchasingController.OnProductsFetched -= OnProductsFetched;
            m_PurchasingController.OnProductsFetchFailed -= OnProductsFetchFailed;
            Debug.Log($"Product fetch failed. Reason: {productFetchFailed.FailureReason}");
            // Optionally, update UI or retry fetching products
        }

        void OnProductsFetched(string storeName, List<Product> products)
        {
            m_PurchasingController.OnProductsFetched -= OnProductsFetched;
            m_PurchasingController.OnProductsFetchFailed -= OnProductsFetchFailed;
            Debug.Log("Products successfully fetched from the store.");
            additionalProductsPanel.SetActive(true); // Show diamond UI
            // Optionally, update UI or refresh product list
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
