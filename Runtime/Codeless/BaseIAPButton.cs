using System;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A GUI component for exposing the current price and allow purchasing of In-App Purchases. Exposes configurable
    /// elements through the Inspector.
    /// </summary>
    public abstract class BaseIAPButton : MonoBehaviour
    {
        protected abstract bool ShouldConsumePurchase();

        protected abstract void OnTransactionsRestored(bool success, string error);
        protected abstract void OnPurchaseComplete(Product purchasedProduct);

        internal abstract void OnInitCompleted();
        protected abstract void AddButtonToCodelessListener();
        protected abstract void RemoveButtonToCodelessListener();
        protected abstract Button GetPurchaseButton();

        void Start()
        {
            var button = GetPurchaseButton();
            var productId = GetProductId();

            if (IsAPurchaseButton())
            {
                if (button)
                {
                    button.onClick.AddListener(PurchaseProduct);
                }

                if (string.IsNullOrEmpty(productId))
                {
                    Debug.LogError("IAPButton productId is empty");
                }
                else if (!CodelessIAPStoreListener.Instance.HasProductInCatalog(productId!))
                {
                    Debug.LogWarning("The product catalog has no product with the ID \"" + productId + "\"");
                }
            }
            else if (IsARestoreButton())
            {
                if (button)
                {
                    button.onClick.AddListener(Restore);
                }
            }
        }

        internal abstract string GetProductId();
        internal abstract bool IsAPurchaseButton();
        protected abstract bool IsARestoreButton();

        void OnEnable()
        {
            if (IsAPurchaseButton())
            {
                AddButtonToCodelessListener();
                if (CodelessIAPStoreListener.initializationComplete)
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
                CodelessIAPStoreListener.Instance.InitiatePurchase(GetProductId());
            }
        }

        protected PurchaseProcessingResult ProcessPurchaseInternal(PurchaseEventArgs args)
        {
            OnPurchaseComplete(args.purchasedProduct);

            return ShouldConsumePurchase() ? PurchaseProcessingResult.Complete : PurchaseProcessingResult.Pending;
        }

        void Restore()
        {
            if (IsARestoreButton())
            {
                if (Application.platform == RuntimePlatform.WSAPlayerX86 ||
                    Application.platform == RuntimePlatform.WSAPlayerX64 ||
                    Application.platform == RuntimePlatform.WSAPlayerARM)
                {
                    CodelessIAPStoreListener.Instance.GetStoreExtensions<IMicrosoftExtensions>()
                        .RestoreTransactions();
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer ||
                         Application.platform == RuntimePlatform.OSXPlayer ||
                         Application.platform == RuntimePlatform.tvOS
#if UNITY_VISIONOS
                         || Application.platform == RuntimePlatform.VisionOS
#endif
                         )
                {
                    CodelessIAPStoreListener.Instance.GetStoreExtensions<IAppleExtensions>()
                        .RestoreTransactions(OnTransactionsRestored);
                }
                else if (Application.platform == RuntimePlatform.Android &&
                         StandardPurchasingModule.Instance().appStore == AppStore.GooglePlay)
                {
                    CodelessIAPStoreListener.Instance.GetStoreExtensions<IGooglePlayStoreExtensions>()
                        .RestoreTransactions(OnTransactionsRestored);
                }
                else
                {
                    Debug.LogWarning(Application.platform +
                        " is not a supported platform for the Codeless IAP restore button");
                }
            }
        }
    }
}
