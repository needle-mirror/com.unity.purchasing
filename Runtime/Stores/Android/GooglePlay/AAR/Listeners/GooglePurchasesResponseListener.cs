using System;
using System.Collections.Generic;
using System.Linq;
using Uniject;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This is C# representation of the Java Class PurchasesResponseListener
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/PurchasesResponseListener">See more</a>
    /// </summary>
    class GooglePurchasesResponseListener : AndroidJavaProxy
    {
        const string k_AndroidPurchasesResponseListenerClassName =
            "com.android.billingclient.api.PurchasesResponseListener";

        readonly Action<IGoogleBillingResult, IEnumerable<AndroidJavaObject>> m_OnQueryPurchasesResponse;
        readonly IUtil m_Util;

        internal GooglePurchasesResponseListener(
            Action<IGoogleBillingResult, IEnumerable<AndroidJavaObject>> onQueryPurchasesResponse, IUtil util)
            : base(k_AndroidPurchasesResponseListenerClassName)
        {
            m_OnQueryPurchasesResponse = onQueryPurchasesResponse;
            m_Util = util;
        }

        [Preserve]
        public void onQueryPurchasesResponse(AndroidJavaObject billingResult, AndroidJavaObject purchases)
        {
            m_Util.RunOnMainThread(() =>
            {
                var purchasesList = purchases.Enumerate().ToList();
                m_OnQueryPurchasesResponse(new GoogleBillingResult(billingResult), purchasesList);

                foreach (var obj in purchasesList)
                {
                    obj?.Dispose();
                }

                billingResult.Dispose();
                purchases?.Dispose();
            });
        }
    }
}
