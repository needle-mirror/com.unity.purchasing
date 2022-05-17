using System;
using System.Collections.Generic;
using System.Linq;
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

        IGoogleLastKnownProductService m_LastKnownProductService;
        IGooglePurchaseCallback m_GooglePurchaseCallback;
        IGoogleCachedQuerySkuDetailsService m_GoogleCachedQuerySkuDetailsService;
        IGooglePurchaseStateEnumProvider m_GooglePurchaseStateEnumProvider;
        IGoogleQueryPurchasesService m_GoogleQueryPurchasesService;

        internal GooglePurchaseUpdatedListener(IGoogleLastKnownProductService googleLastKnownProductService,
            IGooglePurchaseCallback googlePurchaseCallback,
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
        void onPurchasesUpdated(AndroidJavaObject billingResult, AndroidJavaObject javaPurchasesList)
        {
            IGoogleBillingResult result = new GoogleBillingResult(billingResult);
            var purchases = javaPurchasesList.EnumerateAndWrap();
            OnPurchasesUpdated(result, purchases);
        }

        internal void OnPurchasesUpdated(IGoogleBillingResult result, IEnumerable<IAndroidJavaObjectWrapper> purchases)
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

        void HandleResultOkCases(IGoogleBillingResult result, IEnumerable<IAndroidJavaObjectWrapper> purchases)
        {
            if (purchases.Any())
            {
                ApplyOnPurchases(purchases, OnPurchaseOk);
            }
            else if (IsLastProrationModeDeferred())
            {
                OnDeferredProrationUpgradeDowngradeSubscriptionOk();
            }
            else
            {
                HandleErrorCases(result, purchases);
            }
        }

        void HandleErrorCases(IGoogleBillingResult billingResult, IEnumerable<IAndroidJavaObjectWrapper> purchases)
        {
            if (!purchases.Any())
            {
                if (billingResult.responseCode == GoogleBillingResponseCode.ItemAlreadyOwned)
                {
                    m_GooglePurchaseCallback.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            m_LastKnownProductService.GetLastKnownProductId(),
                            PurchaseFailureReason.DuplicateTransaction,
                            billingResult.debugMessage
                        )
                    );
                }
                else if (billingResult.responseCode == GoogleBillingResponseCode.UserCanceled)
                {
                    HandleUserCancelledPurchaseFailure(billingResult);
                }
                else
                {
                    m_GooglePurchaseCallback.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            m_LastKnownProductService.GetLastKnownProductId(),
                            PurchaseFailureReason.Unknown,
                            billingResult.debugMessage + " {M: GPUL.HEC} - Response Code = " + billingResult.responseCode
                        )
                    );
                }
            }
            else
            {
                ApplyOnPurchases(purchases, billingResult, OnPurchaseFailed);
            }
        }

        void HandleUserCancelledPurchaseFailure(IGoogleBillingResult billingResult)
        {
            m_GoogleQueryPurchasesService.QueryPurchases(
                googlePurchases => HandleUserCancelledPurchaseFailure(billingResult, googlePurchases));
        }

        void HandleUserCancelledPurchaseFailure(IGoogleBillingResult billingResult,
            IEnumerable<GooglePurchase> googlePurchases)
        {
            var googlePurchase = googlePurchases.FirstOrDefault(purchase =>
                purchase?.sku == m_LastKnownProductService.GetLastKnownProductId());

            if (googlePurchase != null && !googlePurchase.IsAcknowledged())
            {
                OnPurchaseOk(googlePurchase);
            }
            else
            {
                OnPurchaseCancelled(billingResult);
            }
        }

        void ApplyOnPurchases(IEnumerable<IAndroidJavaObjectWrapper> purchases, Action<GooglePurchase> action)
        {
            foreach (var purchase in purchases)
            {
                GooglePurchase googlePurchase = GooglePurchaseHelper.MakeGooglePurchase(m_GoogleCachedQuerySkuDetailsService.GetCachedQueriedSkus().ToList(), purchase);
                action(googlePurchase);
            }

        }

        void ApplyOnPurchases(IEnumerable<IAndroidJavaObjectWrapper> purchases, IGoogleBillingResult billingResult,
            Action<GooglePurchase, string> action)
        {
            foreach (var purchase in purchases)
            {
                GooglePurchase googlePurchase = GooglePurchaseHelper.MakeGooglePurchase(m_GoogleCachedQuerySkuDetailsService.GetCachedQueriedSkus().ToList(), purchase);
                action(googlePurchase, billingResult.debugMessage);
            }
        }

        bool IsLastProrationModeDeferred()
        {
            return m_LastKnownProductService.GetLastKnownProrationMode() == GooglePlayProrationMode.Deferred;
        }

        void OnPurchaseOk(GooglePurchase googlePurchase)
        {
            if (googlePurchase.purchaseState == m_GooglePurchaseStateEnumProvider.Purchased())
            {
                m_GooglePurchaseCallback.OnPurchaseSuccessful(googlePurchase.sku, googlePurchase.receipt,
                    googlePurchase.purchaseToken);
            }
            else if (googlePurchase.purchaseState == m_GooglePurchaseStateEnumProvider.Pending())
            {
                m_GooglePurchaseCallback.NotifyDeferredPurchase(googlePurchase.sku, googlePurchase.receipt,
                    googlePurchase.purchaseToken);
            }
            else
            {
                m_GooglePurchaseCallback.OnPurchaseFailed(
                    new PurchaseFailureDescription(
                        googlePurchase.sku,
                        PurchaseFailureReason.Unknown,
                        GoogleBillingStrings.errorPurchaseStateUnspecified + " {M: GPUL.OPO}"
                    )
                );
            }
        }

        void OnDeferredProrationUpgradeDowngradeSubscriptionOk()
        {
            m_GooglePurchaseCallback.NotifyDeferredProrationUpgradeDowngradeSubscription(m_LastKnownProductService.GetLastKnownProductId());
        }

        void OnPurchaseCancelled(IGoogleBillingResult billingResult)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    m_LastKnownProductService.GetLastKnownProductId(),
                    PurchaseFailureReason.UserCancelled,
                    billingResult.debugMessage
                )
            );
        }

        void OnPurchaseCancelled(GooglePurchase googlePurchase)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    googlePurchase.sku,
                    PurchaseFailureReason.UserCancelled,
                    GoogleBillingStrings.errorUserCancelled
                )
            );
        }

        void OnPurchaseAlreadyOwned(GooglePurchase googlePurchase)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    googlePurchase.sku,
                    PurchaseFailureReason.DuplicateTransaction,
                    GoogleBillingStrings.errorItemAlreadyOwned
                )
            );
        }

        void OnPurchaseFailed(GooglePurchase googlePurchase, string debugMessage)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    googlePurchase.sku,
                    PurchaseFailureReason.Unknown,
                    debugMessage + " {M: GPUL.OPF}"
                )
            );
        }
    }
}
