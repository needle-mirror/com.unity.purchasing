#define UNITY_UNIFIED_IAP

using System.Collections.Generic;
using System.Linq;
using Stores;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing
{
    class GoogleFetchPurchases : IGoogleFetchPurchases
    {
        IGooglePlayStoreService m_GooglePlayStoreService;
        IGooglePlayStoreFinishTransactionService m_TransactionService;
        IStoreCallback m_StoreCallback;
        internal GoogleFetchPurchases(IGooglePlayStoreService googlePlayStoreService, IGooglePlayStoreFinishTransactionService transactionService)
        {
            m_GooglePlayStoreService = googlePlayStoreService;
            m_TransactionService = transactionService;
        }

        public void SetStoreCallback(IStoreCallback storeCallback)
        {
            m_StoreCallback = storeCallback;
        }

        public void FetchPurchases()
        {
            m_GooglePlayStoreService.FetchPurchases(OnFetchedPurchaseSuccessful);
        }

        void OnFetchedPurchaseSuccessful(List<GooglePurchase> purchases)
        {
            if (purchases != null)
            {
#if UNITY_UNIFIED_IAP
                var purchasedProducts = new List<Product>();

                foreach (var purchase in purchases.Where(purchase => purchase != null).ToList())
                {
                    if (purchase.IsAcknowledged())
                    {
                        var product = m_StoreCallback?.FindProductById(purchase.sku);
                        if (product != null)
                        {
                            product.receipt = purchase.receipt;
                            product.transactionID = purchase.purchaseToken;
                            purchasedProducts.Add(product);
                        }
                    }
                    else
                    {
                        FinishTransaction(purchase);
                    }
                }
                if (purchasedProducts.Count > 0)
                {
                    m_StoreCallback?.OnPurchasesRetrieved(purchasedProducts);
                }
#else
                if (m_StoreCallback.HasMethod("OnPurchasesRetrieved"))
                {
                    var purchasedProducts = new List<Product>();

                    foreach (var purchase in purchases.Where(purchase => purchase != null).ToList())
                    {
                        if (purchase.IsAcknowledged())
                        {
                            var product = m_StoreCallback?.FindProductById(purchase.sku);
                            if (product != null)
                            {
                                product.receipt = purchase.receipt;
                                product.transactionID = purchase.purchaseToken;
                                purchasedProducts.Add(product);
                            }
                        }
                        else
                        {
                            FinishTransaction(purchase);
                        }
                    }

                    if (purchasedProducts.Count > 0)
                    {
                        m_StoreCallback?.InvokeMethod("OnPurchasesRetrieved", new object[]{purchasedProducts});
                    }
                }
                else
                {
                    foreach (GooglePurchase purchase in purchases)
                    {
                        if (purchase != null)
                        {
                            if (purchase.IsAcknowledged())
                            {
                                m_StoreCallback?.OnPurchaseSucceeded(
                                    purchase.sku,
                                    purchase.receipt,
                                    purchase.purchaseToken
                                );
                            }
                            else
                            {
                                FinishTransaction(purchase);
                            }
                        }
                    }
                }
#endif
            }
        }

        void FinishTransaction(GooglePurchase purchase)
        {
            Product product = m_StoreCallback.FindProductById(purchase.sku);
            if (product != null)
            {
                m_TransactionService.FinishTransaction(product.definition, purchase.purchaseToken);
            }
            else
            {
                m_StoreCallback.OnPurchaseFailed(new PurchaseFailureDescription(purchase.sku, PurchaseFailureReason.ProductUnavailable, "Product was not found but was purchased"));
            }
        }
    }
}
