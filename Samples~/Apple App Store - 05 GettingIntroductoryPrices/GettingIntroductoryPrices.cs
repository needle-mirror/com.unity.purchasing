using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;

namespace Samples.Purchasing.AppleAppStore.GettingIntroductoryPrices
{
    public class GettingIntroductoryPrices : MonoBehaviour
    {
        StoreController m_StoreController;

        public string subscriptionProductId = "com.mycompany.mygame.my_vip_pass_subscription";
        public ProductType subscriptionType = ProductType.Subscription;

        public Text isSubscribedText;
        public bool isSubscribed;

        void Awake()
        {
            InitializeIAP();
        }

        async void InitializeIAP()
        {
            m_StoreController = UnityIAPServices.StoreController();

            m_StoreController.OnPurchasePending += OnPurchasePending;
            m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_StoreController.OnPurchaseFailed += OnPurchaseFailed;
            m_StoreController.OnCheckEntitlement += OnCheckEntitlement;
            m_StoreController.OnProductsFetched += OnProductsFetched;

            await m_StoreController.Connect();

            FetchProducts();
        }

        void FetchProducts()
        {
            var initialProductsToFetch = new List<ProductDefinition>
            {
                new(subscriptionProductId, subscriptionType)
            };

            m_StoreController.FetchProducts(initialProductsToFetch);
        }

        void OnPurchasePending(PendingOrder order)
        {
            m_StoreController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(Order order)
        {
            if (order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id == subscriptionProductId)
            {
                Debug.Log($"Purchase confirmed for product: {subscriptionProductId}");
                isSubscribed = true;
            }
        }

        public void OnPurchaseFailed(FailedOrder order)
        {
            Debug.Log($"Purchase failed - Product: '{order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id}'," +
                $" Purchase failure reason: {order.FailureReason}," +
                $" Purchase failure details: {order.Details}");
        }

        void OnCheckEntitlement(Entitlement entitlement)
        {
            if (entitlement.Product.definition.id == subscriptionProductId)
            {
                switch (entitlement.Status)
                {
                    case EntitlementStatus.FullyEntitled:
                        isSubscribed = true;
                        UpdateUI();
                        break;
                    default:
                        isSubscribed = false;
                        UpdateUI();
                        break;
                }
            }
        }

        void OnProductsFetched(List<Product> obj)
        {
            CheckSubscription();
        }

        public void BuySubscription()
        {
            m_StoreController.PurchaseProduct(subscriptionProductId);
        }

        void CheckSubscription()
        {
            var product = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == subscriptionProductId);
            if (product != null)
            {
                m_StoreController.CheckEntitlement(product);
            }
        }

        void UpdateUI()
        {
            isSubscribedText.text = isSubscribed ? "You are subscribed" : "You are not subscribed";
            isSubscribedText.text += $"\nIntroductory Price Information for {subscriptionProductId}:\n{GetIntroductoryPriceForProductId(subscriptionProductId)}";
        }

        string GetIntroductoryPriceForProductId(string productId)
        {
            var introductoryPrices = m_StoreController.AppleStoreExtendedProductService?.GetIntroductoryPriceDictionary();
            var subscriptionIntroductionPriceInfo = introductoryPrices?[productId];

            return subscriptionIntroductionPriceInfo;
        }
    }
}
