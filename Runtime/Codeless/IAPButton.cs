using System;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A GUI component for exposing the current price and allow purchasing of In-App Purchases. Exposes configurable
    /// elements through the Inspector.
    /// </summary>
    /// <seealso cref="CodelessIAPStoreListener"/>
    [Obsolete("IAPButton is deprecated, please use CodelessIAPButton instead.", false)]
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("In-App Purchasing/IAP Button (legacy)", int.MaxValue)]
    [HelpURL("https://docs.unity3d.com/Manual/UnityIAP.html")]
    public class IAPButton : BaseIAPButton
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
        /// Type of event fired after a successful purchase of a product.
        /// </summary>
        [Serializable]
        public class OnPurchaseCompletedEvent : UnityEvent<Product>
        {
        }

        /// <summary>
        /// Type of event fired after a failed purchase of a product.
        /// </summary>
        [Serializable]
        public class OnPurchaseFailedEvent : UnityEvent<Product, PurchaseFailureReason>
        {
        }

        /// <summary>
        /// Type of event fired after a restore transactions was completed.
        /// </summary>
        [Serializable]
        public class OnTransactionsRestoredEvent : UnityEvent<bool, string>
        {
        }

        /// <summary>
        /// Which product identifier to represent. Note this is not a store-specific identifier.
        /// </summary>
        [HideInInspector]
        public string productId = "";

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
        /// Event fired after a restore transactions.
        /// </summary>
        [Tooltip("Event fired after a restore transactions.")]
        public OnTransactionsRestoredEvent onTransactionsRestored = null;

        /// <summary>
        /// Event fired after a successful purchase of this product.
        /// </summary>
        [Tooltip("Event fired after a successful purchase of this product.")]
        public OnPurchaseCompletedEvent onPurchaseComplete = null;

        /// <summary>
        /// Event fired after a failed purchase of this product.
        /// </summary>
        [Tooltip("Event fired after a failed purchase of this product.")]
        public OnPurchaseFailedEvent onPurchaseFailed = null;

        /// <summary>
        /// Displays the localized title from the app store.
        /// </summary>
        [Tooltip("[Optional] Displays the localized title from the app store.")]
        public Text titleText;

        /// <summary>
        /// Displays the localized description from the app store.
        /// </summary>
        [Tooltip("[Optional] Displays the localized description from the app store.")]
        public Text descriptionText;

        /// <summary>
        /// Displays the localized price from the app store.
        /// </summary>
        [Tooltip("[Optional] Displays the localized price from the app store.")]
        public Text priceText;

        internal override string GetProductId()
        {
            return productId;
        }

        internal override bool IsAPurchaseButton()
        {
            return buttonType == ButtonType.Purchase;
        }

        protected override bool IsARestoreButton()
        {
            return buttonType == ButtonType.Restore;
        }

        protected override bool ShouldConsumePurchase()
        {
            return consumePurchase;
        }

        protected override void OnTransactionsRestored(bool success, string error)
        {
            onTransactionsRestored?.Invoke(success, error);
        }

        protected override void OnPurchaseComplete(Product purchasedProduct)
        {
            onPurchaseComplete?.Invoke(purchasedProduct);
        }

        /// <summary>
        /// Invoked on a failed purchase of the product associated with this button
        /// </summary>
        /// <param name="product">The <typeparamref name="Product"/> which failed to purchase</param>
        /// <param name="reason">Information to help developers recover from this failure</param>
        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            onPurchaseFailed?.Invoke(product, reason);
        }

        protected override Button GetPurchaseButton()
        {
            return GetComponent<Button>();
        }

        protected override void AddButtonToCodelessListener()
        {
            CodelessIAPStoreListener.Instance.AddButton(this);
        }

        protected override void RemoveButtonToCodelessListener()
        {
            CodelessIAPStoreListener.Instance.RemoveButton(this);
        }

        internal override void OnInitCompleted()
        {
            UpdateAllTexts();
        }

        void UpdateAllTexts()
        {
            var product = CodelessIAPStoreListener.Instance.GetProduct(productId);
            if (product != null)
            {
                if (titleText != null)
                {
                    titleText.text = product.metadata.localizedTitle;
                }

                if (descriptionText != null)
                {
                    descriptionText.text = product.metadata.localizedDescription;
                }

                if (priceText != null)
                {
                    priceText.text = product.metadata.localizedPriceString;
                }
            }
        }

        /// <summary>
        /// Invoke to process a successful purchase of the product associated with this button.
        /// </summary>
        /// <param name="e">The successful <c>PurchaseEventArgs</c> for the purchase event. </param>
        /// <returns>The result of the successful purchase</returns>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            return ProcessPurchaseInternal(e);
        }
    }
}
