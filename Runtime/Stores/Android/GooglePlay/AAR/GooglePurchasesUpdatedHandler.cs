#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Stores.Android.GooglePlay.AAR.Interfaces;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePurchasesUpdatedHandler : IGooglePurchasesUpdatedHandler
    {
        readonly IGoogleLastKnownProductService m_LastKnownProductService;
        readonly IGooglePurchaseStateEnumProvider m_GooglePurchaseStateEnumProvider;
        readonly IGoogleQueryPurchasesUseCase m_GoogleQueryPurchasesUseCase;
        readonly IGooglePurchaseCallback m_GooglePurchaseCallback;
        IProductCache? m_ProductCache;

        [Preserve]
        internal GooglePurchasesUpdatedHandler(
            IGoogleLastKnownProductService googleLastKnownProductService,
            IGooglePurchaseCallback googlePurchaseCallback,
            IGooglePurchaseStateEnumProvider googlePurchaseStateEnumProvider,
            IGoogleQueryPurchasesUseCase googleQueryPurchasesUseCase)
        {
            m_LastKnownProductService = googleLastKnownProductService;
            m_GooglePurchaseCallback = googlePurchaseCallback;
            m_GooglePurchaseStateEnumProvider = googlePurchaseStateEnumProvider;
            m_GoogleQueryPurchasesUseCase = googleQueryPurchasesUseCase;
        }

        public void SubscribeToPurchasesUpdatedEvent(IGooglePurchasesUpdatedListener purchasesUpdatedListener)
        {
            purchasesUpdatedListener.OnPurchaseUpdated += HandleUpdatedPurchases;
        }

        public void HandleUpdatedPurchases(IGoogleBillingResult result, List<IGooglePurchase> purchases)
        {
            if (result.responseCode == GoogleBillingResponseCode.Ok)
            {
                HandleResultOkCases(result, purchases);
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
                HandleNoPurchasesErrorCase(result);
            }
        }

        void HandleErrorCases(IGoogleBillingResult billingResult, List<IGooglePurchase> purchases)
        {
            if (purchases.Any())
            {
                HandleExistingPurchasesErrorCase(purchases, billingResult);
            }
            else
            {
                HandleNoPurchasesErrorCase(billingResult);
            }
        }

        void HandleExistingPurchasesErrorCase(List<IGooglePurchase> purchases, IGoogleBillingResult billingResult)
        {
            if (billingResult.responseCode == GoogleBillingResponseCode.UserCanceled)
            {
                ApplyOnPurchases(purchases, OnPurchaseCancelled);
            }
            else if (billingResult.responseCode == GoogleBillingResponseCode.ItemAlreadyOwned)
            {
                ApplyOnPurchases(purchases, OnPurchaseAlreadyOwned);
            }
            else
            {
                ApplyOnPurchases(purchases, purchase => OnPurchaseFailedForUnknownReason(purchase, billingResult.debugMessage));
            }
        }

        void HandleNoPurchasesErrorCase(IGoogleBillingResult billingResult)
        {
            switch (billingResult.responseCode)
            {
                case GoogleBillingResponseCode.UserCanceled:
                    HandleUserCancelledPurchaseFailure(billingResult);
                    break;
                case GoogleBillingResponseCode.BillingUnavailable:
                    m_GooglePurchaseCallback.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            m_ProductCache?.FindOrDefault(m_LastKnownProductService.LastKnownProductId) ??
                            Product.CreateUnknownProduct(m_LastKnownProductService.LastKnownProductId),
                            PurchaseFailureReason.PurchasingUnavailable,
                            billingResult.debugMessage + " - Google BillingResponseCode = " + billingResult.responseCode
                        )
                    );
                    break;
                case GoogleBillingResponseCode.ItemAlreadyOwned:
                    m_GooglePurchaseCallback.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            m_ProductCache?.FindOrDefault(m_LastKnownProductService.LastKnownProductId) ??
                            Product.CreateUnknownProduct(m_LastKnownProductService.LastKnownProductId),
                            PurchaseFailureReason.DuplicateTransaction,
                            billingResult.debugMessage + " - Google BillingResponseCode = " + billingResult.responseCode
                        )
                    );
                    break;
                case GoogleBillingResponseCode.Ok:
                    m_GooglePurchaseCallback.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            m_ProductCache?.FindOrDefault(m_LastKnownProductService.LastKnownProductId) ??
                            Product.CreateUnknownProduct(m_LastKnownProductService.LastKnownProductId),
                            PurchaseFailureReason.PurchaseMissing,
                            billingResult.debugMessage + " - onPurchasesUpdated: purchases list is empty - Google BillingResponseCode = " + billingResult.responseCode
                        )
                    );
                    break;
                default:
                    m_GooglePurchaseCallback.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            m_ProductCache?.FindOrDefault(m_LastKnownProductService.LastKnownProductId) ??
                            Product.CreateUnknownProduct(m_LastKnownProductService.LastKnownProductId),
                            PurchaseFailureReason.Unknown,
                            billingResult.debugMessage + " {M: GPUL.HEC} - Google BillingResponseCode = " + billingResult.responseCode
                        )
                    );
                    break;
            }
        }

        async void HandleUserCancelledPurchaseFailure(IGoogleBillingResult billingResult)
        {
            var googlePurchases = await m_GoogleQueryPurchasesUseCase.QueryPurchases();
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

        void OnPurchaseOk(IGooglePurchase googlePurchase)
        {
            if (googlePurchase.purchaseState == m_GooglePurchaseStateEnumProvider.Purchased())
            {
                HandlePurchasedProduct(googlePurchase);
            }
            else if (googlePurchase.purchaseState == m_GooglePurchaseStateEnumProvider.Pending())
            {
                m_GooglePurchaseCallback.NotifyDeferredPurchase(googlePurchase);
            }
            else
            {
                m_GooglePurchaseCallback.OnPurchaseFailed(
                    new PurchaseFailureDescription(
                        m_ProductCache?.FindOrDefault(googlePurchase.purchaseToken) ??
                        Product.CreateUnknownProduct(googlePurchase.purchaseToken),
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
                if (!string.IsNullOrEmpty(m_LastKnownProductService.LastKnownOldProductId) && m_LastKnownProductService.LastKnownOldProductId != m_LastKnownProductService.LastKnownProductId)
                {
                    m_GooglePurchaseCallback.NotifyUpgradeDowngradeSubscription(m_LastKnownProductService.LastKnownProductId);
                }

                m_GooglePurchaseCallback.OnPurchaseSuccessful(googlePurchase);
            }
        }

        bool IsDeferredSubscriptionChange(IGooglePurchase googlePurchase)
        {
            return IsLastReplacementModeDeferred() &&
                googlePurchase.sku == m_LastKnownProductService.LastKnownOldProductId;
        }

        bool IsLastReplacementModeDeferred()
        {
            return m_LastKnownProductService.LastKnownReplacementMode == GooglePlayReplacementMode.Deferred;
        }

        void OnPurchaseCancelled(IGoogleBillingResult billingResult)
        {
            if (!string.IsNullOrEmpty(m_LastKnownProductService.LastKnownOldProductId) && m_LastKnownProductService.LastKnownOldProductId != m_LastKnownProductService.LastKnownProductId)
            {
                m_GooglePurchaseCallback.NotifyUpgradeDowngradeSubscription(m_LastKnownProductService.LastKnownProductId);
            }

            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    m_ProductCache?.FindOrDefault(m_LastKnownProductService.LastKnownProductId) ??
                    Product.CreateUnknownProduct(m_LastKnownProductService.LastKnownProductId),
                    PurchaseFailureReason.UserCancelled,
                    billingResult.debugMessage + " - Google BillingResponseCode = " + billingResult.responseCode
                )
            );
        }

        void OnPurchaseCancelled(IGooglePurchase googlePurchase)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    m_ProductCache?.FindOrDefault(googlePurchase.purchaseToken) ??
                    Product.CreateUnknownProduct(googlePurchase.purchaseToken),
                    PurchaseFailureReason.UserCancelled,
                    GoogleBillingStrings.errorUserCancelled + " - Google BillingResponseCode = " + GoogleBillingResponseCode.UserCanceled
                )
            );
        }

        void OnPurchaseAlreadyOwned(IGooglePurchase googlePurchase)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    m_ProductCache?.FindOrDefault(googlePurchase.purchaseToken) ??
                    Product.CreateUnknownProduct(googlePurchase.purchaseToken),
                    PurchaseFailureReason.DuplicateTransaction,
                    GoogleBillingStrings.errorItemAlreadyOwned + " - Google BillingResponseCode = " + GoogleBillingResponseCode.ItemAlreadyOwned
                )
            );
        }

        void OnPurchaseFailedForUnknownReason(IGooglePurchase googlePurchase, string debugMessage)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    m_ProductCache?.FindOrDefault(googlePurchase.purchaseToken) ??
                    Product.CreateUnknownProduct(googlePurchase.purchaseToken),
                    PurchaseFailureReason.Unknown,
                    debugMessage + " {M: GPUL.OPF}"
                )
            );
        }

        public void SetProductCache(IProductCache? productCache)
        {
            m_ProductCache = productCache;
        }
    }
}
