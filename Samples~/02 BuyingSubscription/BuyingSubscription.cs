using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.Core.BuyingSubscription
{
    public class BuyingSubscription : MonoBehaviour
    {
        PurchasingController m_PurchasingController;
        string m_StoreName;

        // Your subscription ID. It should match the id of your subscription in your store.
        public string subscriptionProductId = "com.mycompany.mygame.my_vip_pass_subscription";

        public Text isSubscribedText;
        public bool isSubscribed = false;

        void Awake()
        {
            InitializeIAP();
        }

        async void InitializeIAP()
        {
            m_StoreName = UnityIAPServices.GetDefaultStore();
            m_PurchasingController = new PurchasingController(m_StoreName);

            m_PurchasingController.OnPurchasePending += OnPurchasePending;
            m_PurchasingController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_PurchasingController.OnCheckEntitlement += OnCheckEntitlement;
            
            m_PurchasingController.OnStoreConnected += OnStoreConnected;
            m_PurchasingController.OnStoreDisconnected += OnStoreDisconnected;

            m_PurchasingController.OnProductsFetched += OnProductsFetched;
            m_PurchasingController.OnProductsFetchFailed += OnProductsFetchFailed;

            await m_PurchasingController.Connect();
        }

        void FetchProducts()
        {
            var products = new List<ProductDefinition>
            {
                new(subscriptionProductId, ProductType.Subscription)
            };
            m_PurchasingController.FetchProducts(products, m_StoreName);
        }

        void OnPurchasePending(string storeName, PendingOrder order)
        {
            m_PurchasingController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(string storeName, Order order)
        {
            CheckSubscription();
        }

        void CheckSubscription()
        {
            var product = m_PurchasingController.GetProducts(m_StoreName).FirstOrDefault(p => p.definition.id == subscriptionProductId);
            m_PurchasingController.CheckEntitlement(product);
        }

        void OnStoreConnected(string storeName)
        {
             Debug.Log($"Store connected.");
             FetchProducts();
        }

        void OnStoreDisconnected(string storeName, StoreConnectionFailureDescription storeConnectionFailureDescription)
        {
            Debug.Log($"Store disconnected. Reason: {storeConnectionFailureDescription}");
            // Optionally, update UI
        }

        void OnProductsFetchFailed(string storeName, ProductFetchFailed productFetchFailed)
        {
            Debug.Log($"Product fetch failed. Reason: {productFetchFailed.FailureReason}");
            // Optionally, update UI or retry fetching products
        }

        void OnProductsFetched(string storeName, List<Product> products)
        {
            Debug.Log("Products successfully fetched from the store.");
            // Optionally, update UI or refresh product list
        }

        void OnCheckEntitlement(string storeName, Entitlement entitlement)
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

        public void BuySubscription()
        {
            m_PurchasingController.PurchaseProduct(subscriptionProductId, m_StoreName);
        }

        void UpdateUI()
        {
            isSubscribedText.text = isSubscribed ? "You are subscribed" : "You are not subscribed";
        }
    }
}
