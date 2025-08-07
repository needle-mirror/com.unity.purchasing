using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.Core.FetchingAdditionalProducts
{
    public class FetchingAdditionalProducts : MonoBehaviour
    {
        StoreController m_StoreController;

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
            m_StoreController = UnityIAPServices.StoreController();
            m_StoreController.OnPurchasePending += OnPurchasePending;
            m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_StoreController.OnStoreDisconnected += OnStoreDisconnected;

            await m_StoreController.Connect();
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
            m_StoreController.OnProductsFetched += OnProductsFetched;
            m_StoreController.OnProductsFetchFailed += OnProductsFetchFailed;
            m_StoreController.FetchProducts(additionalProductsToFetch);
        }

        void OnPurchasePending(PendingOrder order)
        {
            Debug.Log($"Purchase pending - Order: {order}");
            m_StoreController.ConfirmPurchase(order);
            // Optionally, update UI to show pending state
        }

        void OnPurchaseConfirmed(Order order)
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
            m_StoreController.PurchaseProduct(goldProductId);
        }

        public void BuyDiamond()
        {
            m_StoreController.PurchaseProduct(diamondProductId);
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

        void OnStoreDisconnected(StoreConnectionFailureDescription storeConnectionFailureDescription)
        {
            Debug.Log($"Store disconnected. Reason: {storeConnectionFailureDescription}");
            // Optionally, update UI
        }

        void OnProductsFetchFailed(ProductFetchFailed productFetchFailed)
        {
            m_StoreController.OnProductsFetched -= OnProductsFetched;
            m_StoreController.OnProductsFetchFailed -= OnProductsFetchFailed;
            Debug.Log($"Product fetch failed. Reason: {productFetchFailed.FailureReason}");
            // Optionally, update UI or retry fetching products
        }

        void OnProductsFetched(List<Product> products)
        {
            m_StoreController.OnProductsFetched -= OnProductsFetched;
            m_StoreController.OnProductsFetchFailed -= OnProductsFetchFailed;
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
