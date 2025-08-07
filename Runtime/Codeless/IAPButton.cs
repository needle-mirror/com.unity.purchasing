#nullable enable
using System;
using UnityEngine.Events;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;

namespace UnityEngine.Purchasing
{
    //TODO Revamp Codeless for Core API Revamp https://jira.unity3d.com/browse/IAP-2673

    /// <summary>
    /// A GUI component for exposing the current price and allow purchasing of In-App Purchases. Exposes configurable
    /// elements through the Inspector.
    /// </summary>
    /// <seealso cref="CodelessIAPStoreListener"/>
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("In-App Purchasing/IAP Button")]
    [HelpURL("https://docs.unity.com/ugs/en-us/manual/iap/manual/overview")]
    [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
    public class IAPButton : MonoBehaviour
    {
        /// <summary>
        /// The type of this button, can be either a purchase or a restore button.
        /// </summary>
        public enum ButtonType
        {
            /// <summary>
            /// This button will display localized product title and price. Clicking will trigger a purchase.
            /// </summary>
            Purchase,
            /// <summary>
            /// This button will display a static string for restoring previously purchased non-consumable
            /// and subscriptions. Clicking will trigger this restoration process, on supported app stores.
            /// </summary>
            Restore
        }

        /// <summary>
        /// Type of event fired for each fetched product.
        /// </summary>
        [Serializable]
        public class OnProductFetchedEvent : UnityEvent<Product> { }

        /// <summary>
        /// Type of event fired for each product that failed to be fetched.
        /// </summary>
        [Serializable]
        public class OnProductFetchFailedEvent : UnityEvent<ProductDefinition, string> { }

        /// <summary>
        /// Type of event fired for each fetched purchase.
        /// </summary>
        [Serializable]
        public class OnPurchaseFetchedEvent : UnityEvent<Order> { }

        /// <summary>
        /// Type of event fired after a restore transactions was completed.
        /// </summary>
        [Serializable]
        public class OnTransactionsRestoredEvent : UnityEvent<bool, string?> { };

        /// <summary>
        /// Type of event fired after a pending order.
        /// </summary>
        [Serializable]
        public class OnOrderPendingEvent : UnityEvent<PendingOrder> { }

        /// <summary>
        /// Type of event fired after a confirmed order.
        /// </summary>
        [Serializable]
        public class OnOrderConfirmedEvent : UnityEvent<ConfirmedOrder> { }

        /// <summary>
        /// Type of event fired after a failed purchase of a product.
        /// </summary>
        [Serializable]
        public class OnPurchaseFailedEvent : UnityEvent<FailedOrder> { }

        /// <summary>
        /// Type of event fired after deferring to purchase an order.
        /// </summary>
        [Serializable]
        public class OnOrderDeferredEvent : UnityEvent<DeferredOrder> { }

        /// <summary>
        /// Which product identifier to represent. Note this is not a store-specific identifier.
        /// </summary>
        [HideInInspector]
        public string? productId;

        /// <summary>
        /// The type of this button, can be either a purchase or a restore button.
        /// </summary>
        [Tooltip("The type of this button, can be either a purchase or a restore button.")]
        public ButtonType buttonType = ButtonType.Purchase;

        /// <summary>
        /// Consume the product immediately after a successful purchase.
        /// </summary>
        [Tooltip("Consume the product immediately after a successful purchase.")]
        public bool consumePurchase = true;

        /// <summary>
        /// Event fired after fetching a product.
        /// </summary>
        [Tooltip("Event fired after fetching a product.")]
        public OnProductFetchedEvent? onProductFetched;

        /// <summary>
        /// Event fired after failing to fetch a product.
        /// </summary>
        [Tooltip("Event fired after failing to fetch a product.")]
        public OnProductFetchFailedEvent? onProductFetchFailed;

        /// <summary>
        /// Event fired after fetching a purchase.
        /// </summary>
        [Tooltip("Event fired after fetching a purchase.")]
        public OnPurchaseFetchedEvent? onPurchaseFetched;

        /// <summary>
        /// Event fired after a restore transactions.
        /// </summary>
        [Tooltip("Event fired after a restore transactions.")]
        public OnTransactionsRestoredEvent? onTransactionsRestored;

        /// <summary>
        /// Event fired after a pending order.
        /// </summary>
        [Tooltip("Event fired after a pending order.")]
        public OnOrderPendingEvent? onOrderPending;

        /// <summary>
        /// Event fired after a confirmed order.
        /// </summary>
        [Tooltip("Event fired after a confirmed order.")]
        public OnOrderConfirmedEvent? onOrderConfirmed;

        /// <summary>
        /// Event fired after a failed purchase of this product.
        /// </summary>
        [Tooltip("Event fired after a failed purchase of this product.")]
        public OnPurchaseFailedEvent? onPurchaseFailed;

        /// <summary>
        /// Event fired after deferring to purchase an order.
        /// </summary>
        [Tooltip("Event fired after the payment of a purchase was delayed or postponed for this product.")]
        public OnOrderDeferredEvent? onOrderDeferred;

        /// <summary>
        /// Displays the localized title from the app store.
        /// </summary>
        [Tooltip("[Optional] Displays the localized title from the app store.")]
        public Text? titleText;

        /// <summary>
        /// Displays the localized description from the app store.
        /// </summary>
        [Tooltip("[Optional] Displays the localized description from the app store.")]
        public Text? descriptionText;

        /// <summary>
        /// Displays the localized price from the app store.
        /// </summary>
        [Tooltip("[Optional] Displays the localized price from the app store.")]
        public Text? priceText;

        /// <summary>
        /// Invoked for each fetched product.
        /// </summary>
        /// <param name="product">The fetched product.</param>
        public void OnProductFetched(Product product)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Invoked for each failed fetch product.
        /// </summary>
        /// <param name="product">The product that failed to be fetched.</param>
        /// <param name="failureReason">The reason the fetch product failed.</param>
        public void OnProductFetchFailed(ProductDefinition product, string failureReason)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Invoked for each fetched purchase.
        /// </summary>
        /// <param name="order">The fetched purchase.</param>
        public void OnPurchaseFetched(Order order)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Invoked on transactions restored.
        /// </summary>
        /// <param name="success">Indicates if the restore transaction was successful.</param>
        /// <param name="error">When unsuccessful, the error message.</param>
        void OnTransactionsRestored(bool success, string? error)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Invoked on a failed purchase of the product associated with this button
        /// </summary>
        /// <param name="order">The <typeparamref name="Product"/> which failed to purchase</param>
        public void OnOrderPending(PendingOrder order)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Invoked on a failed purchase of the product associated with this button
        /// </summary>
        /// <param name="order">The <typeparamref name="Product"/> which failed to purchase</param>
        public void OnOrderConfirmed(ConfirmedOrder order)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Invoked on a failed order associated with this button
        /// </summary>
        /// <param name="failedOrder">The <typeparamref name="Order"/> which failed to purchase</param>
        public void OnPurchaseFailed(FailedOrder failedOrder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Invoked on a deferred order associated with this button
        /// </summary>
        /// <param name="deferredOrder">The <typeparamref name="DeferredOrder"/> that was deferred</param>
        public void OnOrderDeferred(DeferredOrder deferredOrder)
        {
            throw new NotImplementedException();
        }
    }
}
