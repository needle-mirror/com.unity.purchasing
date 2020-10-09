using System;
using Stores;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing
{
    class GoogleFinishTransactionService : IGoogleFinishTransactionService
    {
        const string k_AndroidConsumeParamsClassName = "com.android.billingclient.api.ConsumeParams";
        static AndroidJavaClass GetConsumeParamsClass()
        {
            return new AndroidJavaClass(k_AndroidConsumeParamsClassName);
        }

        const string k_AndroidAcknowledgePurchaseParamsClassName = "com.android.billingclient.api.AcknowledgePurchaseParams";
        static AndroidJavaClass GetAcknowledgePurchaseParamsClass()
        {
            return new AndroidJavaClass(k_AndroidAcknowledgePurchaseParamsClassName);
        }

        IGoogleBillingClient m_BillingClient;
        IGoogleQueryPurchasesService m_GoogleQueryPurchasesService;
        internal GoogleFinishTransactionService(IGoogleBillingClient billingClient, IGoogleQueryPurchasesService googleQueryPurchasesService)
        {
            m_BillingClient = billingClient;
            m_GoogleQueryPurchasesService = googleQueryPurchasesService;
        }

        public void FinishTransaction(ProductDefinition product, string purchaseToken, Action<ProductDefinition, GooglePurchase, GoogleBillingResult, string> onConsume, Action<ProductDefinition, GooglePurchase, GoogleBillingResult> onAcknowledge)
        {
            m_GoogleQueryPurchasesService.QueryPurchases(purchases =>
            {
                foreach (GooglePurchase purchase in purchases)
                {
                    if (purchase.IsPurchased() && !purchase.IsAcknowledged())
                    {
                        if (product.type == ProductType.Consumable)
                        {
                            ConsumeProduct(product, purchase, purchaseToken, onConsume);
                        }
                        else
                        {
                            AcknowledgePurchase(product, purchase, purchaseToken, onAcknowledge);
                        }
                    }
                }
            });
        }

        void ConsumeProduct(ProductDefinition product, GooglePurchase googlePurchase, string purchaseToken, Action<ProductDefinition, GooglePurchase, GoogleBillingResult, string> onConsume)
        {
            AndroidJavaObject consumeParamsBuilder = GetConsumeParamsClass().CallStatic<AndroidJavaObject>("newBuilder");
            consumeParamsBuilder = consumeParamsBuilder.Call<AndroidJavaObject>("setPurchaseToken", purchaseToken);

            GoogleConsumeResponseListener listener = new GoogleConsumeResponseListener(product, googlePurchase, onConsume);
            m_BillingClient.ConsumeAsync(consumeParamsBuilder.Call<AndroidJavaObject>("build"), listener);
        }

        void AcknowledgePurchase(ProductDefinition product, GooglePurchase googlePurchase, string purchaseToken, Action<ProductDefinition, GooglePurchase, GoogleBillingResult> onAcknowledge)
        {
            AndroidJavaObject acknowledgePurchaseParamsBuilder = GetAcknowledgePurchaseParamsClass().CallStatic<AndroidJavaObject>("newBuilder");
            acknowledgePurchaseParamsBuilder = acknowledgePurchaseParamsBuilder.Call<AndroidJavaObject>("setPurchaseToken", purchaseToken);

            GoogleAcknowledgePurchaseListener listener = new GoogleAcknowledgePurchaseListener(product, googlePurchase, onAcknowledge);
            m_BillingClient.AcknowledgePurchase(acknowledgePurchaseParamsBuilder.Call<AndroidJavaObject>("build"), listener);
        }
    }
}
