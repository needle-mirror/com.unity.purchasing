using System;
using System.Linq;
using Stores;
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

        IGooglePurchaseCallback m_GooglePurchaseCallback;
        IGoogleCachedQuerySkuDetailsService m_GoogleCachedQuerySkuDetailsService;
        internal GooglePurchaseUpdatedListener(IGooglePurchaseCallback googlePurchaseCallback, IGoogleCachedQuerySkuDetailsService googleCachedQuerySkuDetailsService): base(k_AndroidPurchaseListenerClassName)
        {
            m_GooglePurchaseCallback = googlePurchaseCallback;
            m_GoogleCachedQuerySkuDetailsService = googleCachedQuerySkuDetailsService;
        }

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
                            null,
                            PurchaseFailureReason.DuplicateTransaction,
                            billingResult.debugMessage
                        )
                    );
                }
                else
                {
                    m_GooglePurchaseCallback.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            null,
                            PurchaseFailureReason.Unknown,
                            billingResult.debugMessage
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
                m_GooglePurchaseCallback.NotifyDeferredPurchase(googlePurchase.sku, googlePurchase.purchaseToken);
            }
            else
            {
                m_GooglePurchaseCallback.OnPurchaseFailed(
                    new PurchaseFailureDescription(
                        googlePurchase.sku,
                        PurchaseFailureReason.Unknown,
                        GoogleBillingStrings.errorPurchaseStateUnspecified
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
                    debugMessage
                )
            );
        }
    }
}
