#nullable enable

using System;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A GUI component for exposing the current price and allow purchasing of In-App Purchases. Exposes configurable
    /// elements through the Inspector.
    /// </summary>
    /// <seealso cref="CodelessIAPStoreListener"/>
    [AddComponentMenu("In-App Purchasing/IAP Button")]
    [HelpURL("https://docs.unity.com/ugs/en-us/manual/iap/manual/overview")]
    public class CodelessIAPButton : MonoBehaviour
    {
        /// <summary>
        /// Type of event fired after a successful fetching the product information from the store.
        /// </summary>
        [Serializable]
        public class OnProductFetchedEvent : UnityEvent<Product>
        {
        }

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
        public class OnTransactionsRestoredEvent : UnityEvent<bool, string?> { }

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
        /// Type of event fired after a successful purchase of a product.
        /// </summary>
        [Serializable, Obsolete]
        public class OnPurchaseCompletedLegacyEvent : UnityEvent<Product> { }

        /// <summary>
        /// Type of event fired after a failed purchase of a product.
        /// </summary>
        [Serializable, Obsolete]
        public class OnPurchaseFailedLegacyEvent : UnityEvent<Product, PurchaseFailureDescription> { }

        /// <summary>
        /// Which product identifier to represent. Note this is not a store-specific identifier.
        /// </summary>
        [HideInInspector]
        public string? productId;

        /// <summary>
        /// The type of this button, can be either a purchase or a restore button.
        /// </summary>
        [Tooltip("The type of this button, can be either a purchase or a restore button.")]
        public CodelessButtonType buttonType = CodelessButtonType.Purchase;

        /// <summary>
        /// Consume the product immediately after a successful purchase.
        /// </summary>
        [FormerlySerializedAs("consumePurchase")]
        [Tooltip("Automatically confirm the transaction immediately after a successful purchase.")]
        public bool automaticallyConfirmTransaction = true;

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
        /// Event fired after a successful purchase of this product.
        /// </summary>
        [Header("Obsolete Events (for backward compatibility only)")]
        [FormerlySerializedAs("onPurchaseComplete")]
        [Tooltip("Event fired after a successful purchase of this product.")]
        [Obsolete]
        public OnPurchaseCompletedLegacyEvent? onPurchaseCompleteLegacy;

        /// <summary>
        /// Event fired after a failed purchase of this product.
        /// </summary>
        [FormerlySerializedAs("onPurchaseFailed")]
        [Tooltip("Event fired after failing to purchase an order.")]
        [Obsolete]
        public OnPurchaseFailedLegacyEvent? onPurchaseFailedLegacy;

        /// <summary>
        /// Button that triggers purchase.
        /// </summary>
        [Tooltip("Button that triggers purchase.")]
        public Button? button;

        void Start()
        {
            if (IsAPurchaseButton())
            {
                AddPurchaseButtonListener();
            }
            else if (IsARestoreButton())
            {
                AddRestoreButtonListener();
            }
        }

        void AddPurchaseButtonListener()
        {
            GetButton()?.onClick.AddListener(PurchaseProduct);

            if (string.IsNullOrEmpty(productId))
            {
                Debug.LogError("IAPButton productId is empty");
            }
            else if (!CodelessIAPStoreListener.Instance.HasProductInCatalog(productId!))
            {
                Debug.LogWarning("The product catalog has no product with the ID \"" + productId + "\"");
            }
        }

        void AddRestoreButtonListener()
        {
            GetButton()?.onClick.AddListener(Restore);
        }

        void OnEnable()
        {
            if (IsAPurchaseButton())
            {
                AddButtonToCodelessListener();
                if (CodelessIAPStoreListener.Instance.IsInitialized())
                {
                    OnInitCompleted();
                }
            }
        }

        void OnDisable()
        {
            if (IsAPurchaseButton())
            {
                RemoveButtonToCodelessListener();
            }
        }

        void PurchaseProduct()
        {
            if (IsAPurchaseButton())
            {
                CodelessIAPStoreListener.Instance.InitiatePurchase(productId);
            }
        }

        void Restore()
        {
            if (IsARestoreButton())
            {
                UnityIAPServices.DefaultPurchase().RestoreTransactions(OnTransactionsRestored);
            }
        }

        internal bool IsAPurchaseButton()
        {
            return buttonType == CodelessButtonType.Purchase;
        }

        bool IsARestoreButton()
        {
            return buttonType == CodelessButtonType.Restore;
        }

        /// <summary>
        /// Invoked for each fetched product.
        /// </summary>
        /// <param name="product">The fetched product.</param>
        public void OnProductFetched(Product product)
        {
            onProductFetched?.Invoke(product);
        }

        /// <summary>
        /// Invoked for each failed fetch product.
        /// </summary>
        /// <param name="product">The product that failed to be fetched.</param>
        /// <param name="failureReason">The reason the fetch product failed.</param>
        public void OnProductFetchFailed(ProductDefinition product, string failureReason)
        {
            onProductFetchFailed?.Invoke(product, failureReason);
        }

        /// <summary>
        /// Invoked for each fetched purchase.
        /// </summary>
        /// <param name="order">The fetched purchase.</param>
        public void OnPurchaseFetched(Order order)
        {
            onPurchaseFetched?.Invoke(order);
        }

        /// <summary>
        /// Invoked on transactions restored.
        /// </summary>
        /// <param name="success">Indicates if the restore transaction was successful.</param>
        /// <param name="error">When unsuccessful, the error message.</param>
        public void OnTransactionsRestored(bool success, string? error)
        {
            onTransactionsRestored?.Invoke(success, error);
        }

        /// <summary>
        /// Invoked on a failed purchase of the product associated with this button
        /// </summary>
        /// <param name="order">The <typeparamref name="Product"/> which failed to purchase</param>
        public void OnOrderPending(PendingOrder order)
        {
            onOrderPending?.Invoke(order);
            var cartItem = order.CartOrdered.Items().FirstOrDefault(cartItem => cartItem.Product.definition.id == productId);
            if (cartItem != null)
            {
                onPurchaseCompleteLegacy?.Invoke(cartItem.Product);
            }
        }

        /// <summary>
        /// Invoked on a failed purchase of the product associated with this button
        /// </summary>
        /// <param name="order">The <typeparamref name="Product"/> which failed to purchase</param>
        public void OnOrderConfirmed(ConfirmedOrder order)
        {
            onOrderConfirmed?.Invoke(order);
        }

        /// <summary>
        /// Invoked on a failed order associated with this button
        /// </summary>
        /// <param name="failedOrder">The <typeparamref name="Order"/> which failed to purchase</param>
        public void OnPurchaseFailed(FailedOrder failedOrder)
        {
            onPurchaseFailed?.Invoke(failedOrder);
            var cartItem = failedOrder.CartOrdered.Items().FirstOrDefault(cartItem => cartItem.Product.definition.id == productId);
            if (cartItem != null)
            {
                onPurchaseFailedLegacy?.Invoke(cartItem.Product, new PurchaseFailureDescription(cartItem, failedOrder.FailureReason, failedOrder.Details));
            }
        }

        /// <summary>
        /// Invoked on a deferred order associated with this button
        /// </summary>
        /// <param name="deferredOrder">The <typeparamref name="DeferredOrder"/> that was deferred</param>
        public void OnOrderDeferred(DeferredOrder deferredOrder)
        {
            onOrderDeferred?.Invoke(deferredOrder);
        }

        Button? GetButton()
        {
            return button;
        }

        void AddButtonToCodelessListener()
        {
            CodelessIAPStoreListener.Instance.AddButton(this);
        }

        void RemoveButtonToCodelessListener()
        {
            CodelessIAPStoreListener.Instance.RemoveButton(this);
        }

        internal void OnInitCompleted()
        {
            var product = CodelessIAPStoreListener.Instance.GetProduct(productId);
            if (product != null)
            {
                onProductFetched?.Invoke(product);
            }
        }
    }
}
