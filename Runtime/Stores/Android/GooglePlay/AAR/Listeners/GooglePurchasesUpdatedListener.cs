using System;
using System.Collections.Generic;
using System.Linq;
using Uniject;
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
    class GooglePurchasesUpdatedListener : AndroidJavaProxy, IGooglePurchasesUpdatedListener
    {
        const string k_AndroidPurchaseListenerClassName = "com.android.billingclient.api.PurchasesUpdatedListener";

        public event Action<IGoogleBillingResult, List<IGooglePurchase>> OnPurchaseUpdated;

        readonly IGooglePurchaseBuilder m_PurchaseBuilder;
        readonly IUtil m_Util;

        internal GooglePurchasesUpdatedListener(IGooglePurchaseBuilder purchaseBuilder, IUtil util)
            : base(k_AndroidPurchaseListenerClassName)
        {
            m_PurchaseBuilder = purchaseBuilder;
            m_Util = util;
        }

        /// <summary>
        /// Implementation of com.android.billingclient.api.PurchasesUpdatedListener#onPurchasesUpdated
        /// </summary>
        /// <param name="billingResult"></param>
        /// <param name="javaPurchasesList"></param>
        [Preserve]
        public void onPurchasesUpdated(AndroidJavaObject billingResult, AndroidJavaObject javaPurchasesList)
        {
            m_Util.RunOnMainThread(() => HandlePurchasesUpdated(billingResult, javaPurchasesList));
        }

        void HandlePurchasesUpdated(AndroidJavaObject billingResult, AndroidJavaObject javaPurchasesList)
        {
            var purchaseList = javaPurchasesList.Enumerate().ToList();
            IGoogleBillingResult result = new GoogleBillingResult(billingResult);
            var purchases = m_PurchaseBuilder.BuildPurchases(purchaseList).ToList();
            OnPurchaseUpdated?.Invoke(result, purchases);

            foreach (var obj in purchaseList)
            {
                obj?.Dispose();
            }

            billingResult.Dispose();
            javaPurchasesList?.Dispose();
        }
    }
}
