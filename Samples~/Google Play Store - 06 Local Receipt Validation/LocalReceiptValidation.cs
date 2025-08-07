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
        StoreController m_StoreController;

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
            m_StoreController = UnityIAPServices.StoreController();

            m_StoreController.OnPurchasePending += OnPurchasePending;
            m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_StoreController.OnPurchaseFailed += OnPurchaseFailed;
            m_StoreController.OnPurchaseDeferred += OnPurchaseDeferred;

            m_StoreController.OnStoreDisconnected += OnStoreDisconnected;
            Debug.Log("Connecting to store.");
            await m_StoreController.Connect();

            m_StoreController.OnProductsFetchFailed += OnProductsFetchedFailed;
            m_StoreController.OnProductsFetched += OnProductsFetched;
            FetchProducts();

            InitializeValidator();
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
            m_StoreController.PurchaseProduct(goldProductId);
        }

        void OnPurchasePending(PendingOrder order)
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
            m_StoreController.ConfirmPurchase(order);
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

            m_StoreController.FetchProducts(initialProductsToFetch);
        }

        void OnPurchaseConfirmed(Order order)
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

        void OnPurchaseFailed(FailedOrder order)
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

        // Calling StoreController.Connect without a listener on StoreController.OnStoreDisconnected will result in warnings.
        void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            Debug.Log($"Store disconnected details: {description.message}");
        }

        // Calling StoreController.FetchProducts without listeners on StoreController.OnProductsFetched and StoreController.OnProductsFetchedFailed will result in warnings.
        void OnProductsFetched(List<Product> products)
        {
            Debug.Log($"Products fetched successfully for {products.Count} products.");
        }

        void OnProductsFetchedFailed(ProductFetchFailed failure)
        {
            Debug.Log($"Products fetch failed for {failure.FailedFetchProducts.Count} products: {failure.FailureReason}");
        }

        // Calling StoreController.Purchase without listeners on StoreController.OnPurchaseDeferred will result in warnings.
        void OnPurchaseDeferred(DeferredOrder order)
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
