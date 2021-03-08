using System;
using System.Linq;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Utils;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This is C# representation of the Java Class PurchasesUpdatedListener
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/PurchasesUpdatedListener">See more</a>
    /// </summary>
    class GooglePurchaseUpdatedListener: AndroidJavaProxy, IGooglePurchaseUpdatedListener
    {
        const string k_AndroidPurchaseListenerClassName = "com.android.billingclient.api.PurchasesUpdatedListener";

        IGoogleLastKnownProductService m_LastKnownProductService;
        IGooglePurchaseCallback m_GooglePurchaseCallback;
        IGoogleCachedQuerySkuDetailsService m_GoogleCachedQuerySkuDetailsService;
        internal GooglePurchaseUpdatedListener(IGoogleLastKnownProductService googleLastKnownProductService, IGooglePurchaseCallback googlePurchaseCallback, IGoogleCachedQuerySkuDetailsService googleCachedQuerySkuDetailsService): base(k_AndroidPurchaseListenerClassName)
        {
            m_LastKnownProductService = googleLastKnownProductService;
            m_GooglePurchaseCallback = googlePurchaseCallback;
            m_GoogleCachedQuerySkuDetailsService = googleCachedQuerySkuDetailsService;
        }

        /// <summary>
        /// Implementation of com.android.billingclient.api.PurchasesUpdatedListener#onPurchasesUpdated
        /// </summary>
        /// <param name="billingResult"></param>
        /// <param name="purchasesList"></param>
        void onPurchasesUpdated(AndroidJavaObject billingResult, AndroidJavaObject purchasesList)
        {
            GoogleBillingResult result = new GoogleBillingResult(billingResult);
            if (result.responseCode == BillingClientResponseEnum.OK() && purchasesList != null)
            {
                ApplyOnPurchases(purchasesList, OnPurchaseOk);
            }
            else if (result.responseCode == BillingClientResponseEnum.USER_CANCELED() && purchasesList != null)
            {
                ApplyOnPurchases(purchasesList, OnPurchaseCanceled);
            }
            else if (result.responseCode == BillingClientResponseEnum.ITEM_ALREADY_OWNED() && purchasesList != null)
            {
                ApplyOnPurchases(purchasesList, OnPurchaseAlreadyOwned);
            }
            else
            {
                HandleErrorCases(result, purchasesList);
            }
        }

        void HandleErrorCases(GoogleBillingResult billingResult, AndroidJavaObject purchasesList)
        {
            if (purchasesList == null)
            {
                if (billingResult.responseCode == BillingClientResponseEnum.ITEM_ALREADY_OWNED())
                {
                    m_GooglePurchaseCallback.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            m_LastKnownProductService.GetLastKnownProductId(),
                            PurchaseFailureReason.DuplicateTransaction,
                            billingResult.debugMessage
                        )
                    );
                }
                else if (billingResult.responseCode == BillingClientResponseEnum.USER_CANCELED())
                {
                    m_GooglePurchaseCallback.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            m_LastKnownProductService.GetLastKnownProductId(),
                            PurchaseFailureReason.UserCancelled,
                            billingResult.debugMessage
                        )
                    );
                }
                else
                {
                    m_GooglePurchaseCallback.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            m_LastKnownProductService.GetLastKnownProductId(),
                            PurchaseFailureReason.Unknown,
                            billingResult.debugMessage + " {M: GPUL.HEC}"
                        )
                    );
                }
            }
            else
            {
                ApplyOnPurchases(purchasesList, billingResult, OnPurchaseFailed);
            }
        }

        void ApplyOnPurchases(AndroidJavaObject purchasesList, Action<GooglePurchase> action)
        {
            int size = purchasesList.Call<int>("size");
            for (int index = 0; index < size; index++)
            {
                AndroidJavaObject purchase = purchasesList.Call<AndroidJavaObject>("get", index);
                GooglePurchase googlePurchase = GooglePurchaseHelper.MakeGooglePurchase(m_GoogleCachedQuerySkuDetailsService.GetCachedQueriedSkus().ToList(), purchase);
                action(googlePurchase);
            }
        }

        void ApplyOnPurchases(AndroidJavaObject purchasesList, GoogleBillingResult billingResult, Action<GooglePurchase, string> action)
        {
            int size = purchasesList.Call<int>("size");
            for (int index = 0; index < size; index++)
            {
                AndroidJavaObject purchase = purchasesList.Call<AndroidJavaObject>("get", index);
                GooglePurchase googlePurchase = GooglePurchaseHelper.MakeGooglePurchase(m_GoogleCachedQuerySkuDetailsService.GetCachedQueriedSkus().ToList(), purchase);
                action(googlePurchase, billingResult.debugMessage);
            }
        }

        void OnPurchaseOk(GooglePurchase googlePurchase)
        {
            if (googlePurchase.purchaseState == GooglePurchaseStateEnum.Purchased())
            {
                m_GooglePurchaseCallback.OnPurchaseSuccessful(googlePurchase.sku, googlePurchase.receipt, googlePurchase.purchaseToken);
            }
            else if (googlePurchase.purchaseState == GooglePurchaseStateEnum.Pending())
            {
                m_GooglePurchaseCallback.NotifyDeferredPurchase(googlePurchase.sku, googlePurchase.receipt, googlePurchase.purchaseToken);
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

        void OnPurchaseCanceled(GooglePurchase googlePurchase)
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
