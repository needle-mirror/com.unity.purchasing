using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// An invisible GUI component for initializing Unity IAP and processing lifecycle events.
    /// </summary>
    /// <seealso cref="CodelessIAPStoreListener"/>
    [AddComponentMenu("In-App Purchasing/IAP Listener")]
    [HelpURL("https://docs.unity.com/ugs/en-us/manual/iap/manual/overview")]
    public class IAPListener : MonoBehaviour
    {
        /// <summary>
        /// Type of event fired after fetching products.
        /// </summary>
        [Serializable]
        public class OnProductsFetchedEvent : UnityEvent<List<Product>> { }

        /// <summary>
        /// Type of event fired after failing to fetch products.
        /// </summary>
        [Serializable]
        public class OnProductsFetchFailedEvent : UnityEvent<ProductFetchFailed> { }

        /// <summary>
        /// Type of event fired after fetching purchases.
        /// </summary>
        [Serializable]
        public class OnPurchasesFetchedEvent : UnityEvent<Orders> { }

        /// <summary>
        /// Type of event fired after failing to fetch purchases.
        /// </summary>
        [Serializable]
        public class OnPurchasesFetchFailureEvent : UnityEvent<PurchasesFetchFailureDescription> { }

        /// <summary>
        /// Type of event fired after updating a pending order.
        /// </summary>
        [Serializable]
        public class OnOrderPendingEvent : UnityEvent<PendingOrder> { }

        /// <summary>
        /// Type of event fired after updating a confirmed order.
        /// </summary>
        [Serializable]
        public class OnOrderConfirmedEvent : UnityEvent<ConfirmedOrder> { }

        /// <summary>
        /// Type of event fired after failing to purchase an order.
        /// </summary>
        [Serializable]
        public class OnPurchaseFailedEvent : UnityEvent<FailedOrder> { }

        /// <summary>
        /// Type of event fired after deferring to purchase an order.
        /// </summary>
        [Serializable]
        public class OnOrderDeferredEvent : UnityEvent<DeferredOrder> { }

        /// <summary>
        /// Legacy event type for fetching products, deprecated in favor of OnProductsFetchedEvent.
        /// </summary>
        [Serializable, Obsolete]
        public class OnProductsFetchedLegacyEvent : UnityEvent<ProductCollection> { }

        /// <summary>
        /// Legacy event type for fetching purchases, deprecated in favor of OnPurchasesFetchedEvent.
        /// </summary>
        [Serializable, Obsolete]
        public class OnPurchaseCompletedLegacyEvent : UnityEvent<Product> { }

        /// <summary>
        /// Legacy event type for purchase failure, deprecated in favor of OnPurchaseFailedEvent.
        /// </summary>
        [Serializable, Obsolete]
        public class OnPurchaseFailedLegacyEvent : UnityEvent<Product, PurchaseFailureReason> { }

        /// <summary>
        /// Legacy event type for detailed purchase failure, deprecated in favor of OnPurchaseFailedEvent.
        /// </summary>
        [Serializable, Obsolete]
        public class OnPurchaseDetailedFailedLegacyEvent : UnityEvent<Product, PurchaseFailureDescription> { }

        /// <summary>
        /// Consume successful purchases immediately.
        /// </summary>
        [Tooltip("Automatically confirm the transaction immediately after a successful purchase.")]
        public bool automaticallyConfirmTransaction = true;

        /// <summary>
        /// Preserve this GameObject when a new scene is loaded.
        /// </summary>
        [Tooltip("Preserve this GameObject when a new scene is loaded.")]
        public bool dontDestroyOnLoad = true;

        /// <summary>
        /// Event fired after fetching products.
        /// </summary>
        [Tooltip("Event fired after fetching products.")]
        public OnProductsFetchedEvent onProductsFetched;

        /// <summary>
        /// Event fired after failing to fetch products.
        /// </summary>
        [Tooltip("Event fired after failing to fetch products.")]
        public OnProductsFetchFailedEvent onProductsFetchFailed;

        /// <summary>
        /// Event fired after fetching purchases.
        /// </summary>
        [Tooltip("Event fired after fetching purchases.")]
        public OnPurchasesFetchedEvent onPurchasesFetched;

        /// <summary>
        /// Event fired after failing to fetch purchases.
        /// </summary>
        [Tooltip("Event fired after failing to fetch purchases.")]
        public OnPurchasesFetchFailureEvent onPurchasesFetchFailure;

        /// <summary>
        /// Event fired after updating a pending order.
        /// </summary>
        [Tooltip("Event fired after updating a pending order.")]
        public OnOrderPendingEvent onOrderPending;

        /// <summary>
        /// Event fired after updating a confirmed order.
        /// </summary>
        [Tooltip("Event fired after updating a confirmed order.")]
        public OnOrderConfirmedEvent onOrderConfirmed;

        /// <summary>
        /// Event fired after failing to purchase an order.
        /// </summary>
        [Tooltip("Event fired after failing to purchase an order.")]
        public OnPurchaseFailedEvent onPurchaseFailed;

        /// <summary>
        /// Event fired after deferring to purchase an order.
        /// </summary>
        [Tooltip("Event fired after the payment of a purchase was delayed or postponed.")]
        public OnOrderDeferredEvent onOrderDeferred;

        /// <summary>
        /// Legacy event for fetching products, deprecated in favor of OnProductsFetchedEvent.
        /// </summary>
        [Header("Obsolete Events (for backward compatibility only)")]
        [FormerlySerializedAs("onProductsFetched")]
        [Tooltip("Event fired after a successful fetching the products from the store.")]
        [Obsolete]
        public OnProductsFetchedLegacyEvent onProductsFetchedLegacy;

        /// <summary>
        /// Legacy event for purchase completion, deprecated in favor of OnPurchasesFetchedEvent.
        /// </summary>
        [FormerlySerializedAs("onPurchaseComplete")]
        [Tooltip("Event fired after a successful purchase of this product.")]
        [Obsolete]
        public OnPurchaseCompletedLegacyEvent onPurchaseCompleteLegacy;

        /// <summary>
        /// Legacy event for purchase failure, deprecated in favor of OnPurchaseFailedEvent.
        /// </summary>
        [FormerlySerializedAs("onPurchaseFailed")]
        [Tooltip("Event fired after failing to purchase an order.")]
        [Obsolete]
        public OnPurchaseFailedLegacyEvent onPurchaseFailedLegacy;

        /// <summary>
        /// Legacy event for detailed purchase failure, deprecated in favor of OnPurchaseFailedEvent.
        /// </summary>
        [FormerlySerializedAs("onPurchaseDetailedFailedEvent")]
        [Tooltip("Event fired after failing to purchase an order.")]
        [Obsolete]
        public OnPurchaseDetailedFailedLegacyEvent onPurchaseDetailedFailedLegacy;

        void OnEnable()
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            CodelessIAPStoreListener.Instance.AddListener(this);
        }

        void OnDisable()
        {
            CodelessIAPStoreListener.Instance.RemoveListener(this);
        }

        /// <summary>
        /// Invoked when fetching products
        /// </summary>
        /// <param name="products">The <typeparamref name="Product"/> which were fetched</param>
        public void OnProductsFetched(List<Product> products)
        {
            onProductsFetched.Invoke(products);
            onProductsFetchedLegacy.Invoke(new ProductCollection());
        }

        /// <summary>
        /// Invoked when failing to fetch products
        /// </summary>
        /// <param name="productFetchFailed">The <typeparamref name="productFetchFailed"/> containing details about the product fetch failure</param>
        public void OnProductsFetchFailed(ProductFetchFailed productFetchFailed)
        {
            onProductsFetchFailed.Invoke(productFetchFailed);
        }

        /// <summary>
        /// Invoked when fetching purchases
        /// </summary>
        /// <param name="orders">The fetched purchase <typeparamref name="Orders"/></param>
        public void OnPurchasesFetched(Orders orders)
        {
            onPurchasesFetched.Invoke(orders);
        }

        /// <summary>
        /// Invoked when failing to fetch purchases
        /// </summary>
        /// <param name="purchasesFetchFailureDescription">The <typeparamref name="PurchasesFetchFailureDescription"/> containing details about the purchases fetch failure</param>
        public void OnPurchasesFetchFailure(PurchasesFetchFailureDescription purchasesFetchFailureDescription)
        {
            onPurchasesFetchFailure.Invoke(purchasesFetchFailureDescription);
        }

        /// <summary>
        /// Invoked when updating a pending order
        /// </summary>
        /// <param name="pendingOrder">The <typeparamref name="PendingOrder"/> that was updated</param>
        public void OnOrderPending(PendingOrder pendingOrder)
        {
            onOrderPending.Invoke(pendingOrder);
            onPurchaseCompleteLegacy.Invoke(pendingOrder.CartOrdered.Items().FirstOrDefault()?.Product);
        }

        /// <summary>
        /// Invoked when updating a confirmed order.
        /// </summary>
        /// <param name="confirmedOrder">The <typeparamref name="ConfirmedOrder"/> that was updated</param>
        public void OnOrderConfirmed(ConfirmedOrder confirmedOrder)
        {
            onOrderConfirmed?.Invoke(confirmedOrder);
        }

        /// <summary>
        /// Invoked on failing to purchase an order.
        /// </summary>
        /// <param name="failedOrder">The <typeparamref name="FailedOrder"/> which failed to purchase</param>
        public void OnPurchaseFailed(FailedOrder failedOrder)
        {
            onPurchaseFailed.Invoke(failedOrder);

            var product = failedOrder.CartOrdered.Items().FirstOrDefault()?.Product;
            onPurchaseFailedLegacy.Invoke(product, failedOrder.FailureReason);
            onPurchaseDetailedFailedLegacy.Invoke(product, new PurchaseFailureDescription(failedOrder.CartOrdered.Items().FirstOrDefault(), failedOrder.FailureReason, failedOrder.Details));
        }

        /// <summary>
        /// Invoked on deferring to purchase an order.
        /// </summary>
        /// <param name="deferredOrder">The <typeparamref name="DeferredOrder"/> that was deferred</param>
        public void OnOrderDeferred(DeferredOrder deferredOrder)
        {
            onOrderDeferred.Invoke(deferredOrder);
        }
    }
}
