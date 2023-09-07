using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Utils;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This is C# representation of the Java Class PurchasesUpdatedListener
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/PurchasesUpdatedListener">See more</a>
    /// </summary>
    class GooglePurchaseUpdatedListener : AndroidJavaProxy, IGooglePurchaseUpdatedListener
    {
        const string k_AndroidPurchaseListenerClassName = "com.android.billingclient.api.PurchasesUpdatedListener";
        readonly IGoogleLastKnownProductService m_LastKnownProductService;
        readonly IGooglePurchaseCallback m_GooglePurchaseCallback;
        readonly IGooglePurchaseBuilder m_PurchaseBuilder;
        readonly IGoogleCachedQuerySkuDetailsService m_GoogleCachedQuerySkuDetailsService;
        readonly IGooglePurchaseStateEnumProvider m_GooglePurchaseStateEnumProvider;
        IGoogleQueryPurchasesService m_GoogleQueryPurchasesService;

        internal GooglePurchaseUpdatedListener(IGoogleLastKnownProductService googleLastKnownProductService,
            IGooglePurchaseCallback googlePurchaseCallback, IGooglePurchaseBuilder purchaseBuilder,
            IGoogleCachedQuerySkuDetailsService googleCachedQuerySkuDetailsService,
            IGooglePurchaseStateEnumProvider googlePurchaseStateEnumProvider,
            IGoogleQueryPurchasesService googleQueryPurchasesService = null)
            : base(k_AndroidPurchaseListenerClassName)
        {
            m_LastKnownProductService = googleLastKnownProductService;
            m_GooglePurchaseCallback = googlePurchaseCallback;
            m_GoogleCachedQuerySkuDetailsService = googleCachedQuerySkuDetailsService;
            m_GooglePurchaseStateEnumProvider = googlePurchaseStateEnumProvider;
            m_GoogleQueryPurchasesService = googleQueryPurchasesService;
            m_PurchaseBuilder = purchaseBuilder;
        }

        public void SetGoogleQueryPurchaseService(IGoogleQueryPurchasesService googleFetchPurchases)
        {
            m_GoogleQueryPurchasesService = googleFetchPurchases;
        }

        /// <summary>
        /// Implementation of com.android.billingclient.api.PurchasesUpdatedListener#onPurchasesUpdated
        /// </summary>
        /// <param name="billingResult"></param>
        /// <param name="javaPurchasesList"></param>
        [Preserve]
        public void onPurchasesUpdated(AndroidJavaObject billingResult, AndroidJavaObject javaPurchasesList)
        {
            IGoogleBillingResult result = new GoogleBillingResult(billingResult);
            var purchases = m_PurchaseBuilder.BuildPurchases(javaPurchasesList.EnumerateAndWrap()).ToList();
            OnPurchasesUpdated(result, purchases);
        }

        internal void OnPurchasesUpdated(IGoogleBillingResult result, List<IGooglePurchase> purchases)
        {
            if (result.responseCode == GoogleBillingResponseCode.Ok)
            {
                HandleResultOkCases(result, purchases);
            }
            else if (result.responseCode == GoogleBillingResponseCode.UserCanceled && purchases.Any())
            {
                ApplyOnPurchases(purchases, OnPurchaseCancelled);
            }
            else if (result.responseCode == GoogleBillingResponseCode.ItemAlreadyOwned && purchases.Any())
            {
                ApplyOnPurchases(purchases, OnPurchaseAlreadyOwned);
            }
            else
            {
                HandleErrorCases(result, purchases);
            }
        }

        void HandleResultOkCases(IGoogleBillingResult result, List<IGooglePurchase> purchases)
        {
            if (purchases.Any())
            {
                ApplyOnPurchases(purchases, OnPurchaseOk);
            }
            else
            {
                HandleErrorCases(result, purchases);
            }
        }

        void HandleErrorCases(IGoogleBillingResult billingResult, List<IGooglePurchase> purchases)
        {
            if (!purchases.Any())
            {
                HandleErrorGoogleBillingResult(billingResult);
            }
            else
            {
                ApplyOnPurchases(purchases, billingResult, OnPurchaseFailed);
            }
        }

        void HandleErrorGoogleBillingResult(IGoogleBillingResult billingResult)
        {
            switch (billingResult.responseCode)
            {
                case GoogleBillingResponseCode.ItemAlreadyOwned:
                    m_GooglePurchaseCallback.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            m_LastKnownProductService.LastKnownProductId,
                            PurchaseFailureReason.DuplicateTransaction,
                            billingResult.debugMessage + " - Google BillingResponseCode = " + billingResult.responseCode
                        )
                    );
                    break;
                case GoogleBillingResponseCode.BillingUnavailable:
                    m_GooglePurchaseCallback.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            m_LastKnownProductService.LastKnownProductId,
                            PurchaseFailureReason.PurchasingUnavailable,
                            billingResult.debugMessage + " - Google BillingResponseCode = " + billingResult.responseCode
                        )
                    );
                    break;
                case GoogleBillingResponseCode.UserCanceled:
                    HandleUserCancelledPurchaseFailure(billingResult);
                    break;
                default:
                    m_GooglePurchaseCallback.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            m_LastKnownProductService.LastKnownProductId,
                            PurchaseFailureReason.Unknown,
                            billingResult.debugMessage + " {M: GPUL.HEC} - Google BillingResponseCode = " + billingResult.responseCode
                        )
                    );
                    break;
            }
        }

        async void HandleUserCancelledPurchaseFailure(IGoogleBillingResult billingResult)
        {
            var googlePurchases = await m_GoogleQueryPurchasesService.QueryPurchases();
            HandleUserCancelledPurchaseFailure(billingResult, googlePurchases);
        }

        void HandleUserCancelledPurchaseFailure(IGoogleBillingResult billingResult,
            List<IGooglePurchase> googlePurchases)
        {
            var googlePurchase = googlePurchases.FirstOrDefault(purchase =>
                purchase?.sku == m_LastKnownProductService.LastKnownProductId);

            if (googlePurchase != null && !googlePurchase.IsAcknowledged())
            {
                OnPurchaseOk(googlePurchase);
            }
            else
            {
                OnPurchaseCancelled(billingResult);
            }
        }

        void ApplyOnPurchases(List<IGooglePurchase> purchases, Action<IGooglePurchase> action)
        {
            foreach (var purchase in purchases)
            {
                action(purchase);
            }
        }

        void ApplyOnPurchases(IEnumerable<IGooglePurchase> purchases, IGoogleBillingResult billingResult, Action<IGooglePurchase, string> action)
        {
            foreach (var purchase in purchases)
            {
                action(purchase, billingResult.debugMessage);
            }
        }

        void OnPurchaseOk(IGooglePurchase googlePurchase)
        {
            if (googlePurchase.purchaseState == m_GooglePurchaseStateEnumProvider.Purchased())
            {
                HandlePurchasedProduct(googlePurchase);
            }
            else if (googlePurchase.purchaseState == m_GooglePurchaseStateEnumProvider.Pending())
            {
                m_GooglePurchaseCallback.NotifyDeferredPurchase(googlePurchase, googlePurchase.receipt, googlePurchase.purchaseToken);
            }
            else
            {
                m_GooglePurchaseCallback.OnPurchaseFailed(
                    new PurchaseFailureDescription(
                        googlePurchase.purchaseToken,
                        PurchaseFailureReason.Unknown,
                        GoogleBillingStrings.errorPurchaseStateUnspecified + " {M: GPUL.OPO}"
                    )
                );
            }
        }

        void HandlePurchasedProduct(IGooglePurchase googlePurchase)
        {
            if (IsDeferredSubscriptionChange(googlePurchase))
            {
                m_GooglePurchaseCallback.NotifyDeferredProrationUpgradeDowngradeSubscription(m_LastKnownProductService.LastKnownProductId);
            }
            else
            {
                m_GooglePurchaseCallback.OnPurchaseSuccessful(googlePurchase, googlePurchase.receipt, googlePurchase.purchaseToken);
            }
        }

        bool IsDeferredSubscriptionChange(IGooglePurchase googlePurchase)
        {
            return IsLastProrationModeDeferred() &&
                   googlePurchase.sku == m_LastKnownProductService.LastKnownOldProductId;
        }

        bool IsLastProrationModeDeferred()
        {
            return m_LastKnownProductService.LastKnownProrationMode == GooglePlayProrationMode.Deferred;
        }

        void OnPurchaseCancelled(IGoogleBillingResult billingResult)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    m_LastKnownProductService.LastKnownProductId,
                    PurchaseFailureReason.UserCancelled,
                    billingResult.debugMessage
                )
            );
        }

        void OnPurchaseCancelled(IGooglePurchase googlePurchase)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    googlePurchase.purchaseToken,
                    PurchaseFailureReason.UserCancelled,
                    GoogleBillingStrings.errorUserCancelled
                )
            );
        }

        void OnPurchaseAlreadyOwned(IGooglePurchase googlePurchase)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    googlePurchase.purchaseToken,
                    PurchaseFailureReason.DuplicateTransaction,
                    GoogleBillingStrings.errorItemAlreadyOwned
                )
            );
        }

        void OnPurchaseFailed(IGooglePurchase googlePurchase, string debugMessage)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    googlePurchase.purchaseToken,
                    PurchaseFailureReason.Unknown,
                    debugMessage + " {M: GPUL.OPF}"
                )
            );
        }
    }
}
