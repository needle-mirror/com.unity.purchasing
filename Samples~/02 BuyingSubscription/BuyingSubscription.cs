using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.Core.BuyingSubscription
{
    public class BuyingSubscription : MonoBehaviour
    {
        StoreController m_StoreController;

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
            m_StoreController = UnityIAPServices.StoreController();

            m_StoreController.OnPurchasePending += OnPurchasePending;
            m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_StoreController.OnCheckEntitlement += OnCheckEntitlement;
            m_StoreController.OnStoreDisconnected += OnStoreDisconnected;

            await m_StoreController.Connect();
            FetchProducts();
        }

        void FetchProducts()
        {
            var products = new List<ProductDefinition>
            {
                new(subscriptionProductId, ProductType.Subscription)
            };
            m_StoreController.OnProductsFetched += OnProductsFetched;
            m_StoreController.OnProductsFetchFailed += OnProductsFetchFailed;
            m_StoreController.FetchProducts(products);
        }

        void OnPurchasePending(PendingOrder order)
        {
            m_StoreController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(Order order)
        {
            CheckSubscription();
        }

        void CheckSubscription()
        {
            var product = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == subscriptionProductId);
            m_StoreController.CheckEntitlement(product);
        }

        void OnStoreDisconnected(StoreConnectionFailureDescription storeConnectionFailureDescription)
        {
            Debug.Log($"Store disconnected. Reason: {storeConnectionFailureDescription}");
            // Optionally, update UI
        }

        void OnProductsFetchFailed(ProductFetchFailed productFetchFailed)
        {
            Debug.Log($"Product fetch failed. Reason: {productFetchFailed.FailureReason}");
            // Optionally, update UI or retry fetching products
        }

        void OnProductsFetched(List<Product> products)
        {
            Debug.Log("Products successfully fetched from the store.");
            // Optionally, update UI or refresh product list
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

        public void BuySubscription()
        {
            m_StoreController.PurchaseProduct(subscriptionProductId);
        }

        void UpdateUI()
        {
            isSubscribedText.text = isSubscribed ? "You are subscribed" : "You are not subscribed";
        }
    }
}
