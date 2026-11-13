using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;
using UnityEngine.UI;

namespace Samples.Purchasing.Core.LocalReceiptValidation
{
    public class LocalReceiptValidation : MonoBehaviour
    {
        PurchasingController m_PurchasingController;
        string m_StoreName;

        CrossPlatformValidator m_Validator = null;

        //Your products IDs. They should match the ids of your products in your store.
        public string goldProductId = "com.mycompany.mygame.gold1";
        public ProductType productType = ProductType.Consumable;

        public Text GoldCountText;

        public UserWarning userWarning;

        int m_GoldCount;

        void Awake()
        {
            InitializeIAP();
        }

        void Start()
        {
            userWarning.Clear();
            UpdateUI();
        }
        
        static bool IsGooglePlayStoreSelected()
        {
            var currentAppStore = StandardPurchasingModule.Instance().appStore;
            return currentAppStore == AppStore.GooglePlay;
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

        void InitializeValidator()
        {
            if (IsGooglePlayStoreSelected())
            {
#if !UNITY_EDITOR
                m_Validator = new CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier);
#endif
            }
            else
            {
                userWarning.WarnInvalidStore(StandardPurchasingModule.Instance().appStore);
            }
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
                Debug.Log("Could not find product in order. Unable to validate receipt or grant purchase.");
            }

            var isPurchaseValid = IsPurchaseValid(order);

            if (isPurchaseValid)
            {
                //Add the purchased product to the players inventory
                Debug.Log("Valid receipt, unlocking content.");
                UnlockContent(product);
            }
            else
            {
                Debug.Log("Invalid receipt, not unlocking content.");
            }

            Debug.Log($"Purchase complete - Product: {GetIdFromProduct(product)}");

            // We call CompletePurchase, informing Unity IAP that the processing on our side is done and the transaction can be closed.
            m_PurchasingController.ConfirmPurchase(order);
        }

        bool IsPurchaseValid(Order order)
        {
            //If the validator doesn't support the current store, we assume the purchase is valid
            if (IsGooglePlayStoreSelected())
            {
                try
                {
                    var result = m_Validator.Validate(order.Info.Receipt);

                    //The validator returns parsed receipts.
                    LogReceipts(result);
                }

                //If the purchase is deemed invalid, the validator throws an IAPSecurityException.
                catch (IAPSecurityException reason)
                {
                    Debug.Log($"Invalid receipt: {reason}");
                    return false;
                }
            }

            return true;
        }

        void UnlockContent(Product product)
        {
            if (product.definition.id == goldProductId)
            {
                AddGold();
            }
        }

        void FetchProducts()
        {
            var initialProductsToFetch = new List<ProductDefinition>
            {
                new(goldProductId, productType),
            };

            m_PurchasingController.FetchProducts(initialProductsToFetch, m_StoreName);
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

        // Calling PurchasingController.Connect without a listener on PurchasingController.OnStoreConnected will result in warnings.
        void OnStoreConnected(string storeName)
        {
            Debug.Log($"Store connected.");

            FetchProducts();

            InitializeValidator();
        }

        // Calling PurchasingController.Connect without a listener on PurchasingController.OnStoreDisconnected will result in warnings.
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

        void AddGold()
        {
            m_GoldCount++;
            UpdateUI();
        }

        void UpdateUI()
        {
            GoldCountText.text = $"Your Gold: {m_GoldCount}";
        }

        static void LogReceipts(IEnumerable<IPurchaseReceipt> receipts)
        {
            Debug.Log("Receipt is valid. Contents:");
            foreach (var receipt in receipts)
            {
                LogReceipt(receipt);
            }
        }

        static void LogReceipt(IPurchaseReceipt receipt)
        {
            Debug.Log($"Product ID: {receipt.productID}\n" +
                $"Purchase Date: {receipt.purchaseDate}\n" +
                $"Transaction ID: {receipt.transactionID}");

            if (receipt is GooglePlayReceipt googleReceipt)
            {
                Debug.Log($"Purchase State: {googleReceipt.purchaseState}\n" +
                    $"Purchase Token: {googleReceipt.purchaseToken}");
            }
        }
    }
}
