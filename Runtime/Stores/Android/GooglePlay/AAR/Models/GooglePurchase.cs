#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Utils;

namespace UnityEngine.Purchasing.Models
{
    /// <summary>
    /// This is C# representation of the Java Class Purchase
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase">See more</a>
    /// </summary>
    class GooglePurchase : IGooglePurchase
    {
        public IAndroidJavaObjectWrapper javaPurchase { get; }
        public int purchaseState { get; }
        public List<string> skus { get; }
        public string orderId { get; }
        public string receipt { get; }
        public string signature { get; }
        public string originalJson { get; }
        public string purchaseToken { get; }

        public string? sku => skus.FirstOrDefault();

        internal GooglePurchase(IAndroidJavaObjectWrapper purchase, IEnumerable<IAndroidJavaObjectWrapper> skuDetails)
        {
            javaPurchase = purchase;
            purchaseState = purchase.Call<int>("getPurchaseState");
            skus = purchase.Call<AndroidJavaObject>("getSkus").Enumerate<string>().ToList();
            orderId = purchase.Call<string>("getOrderId");
            originalJson = purchase.Call<string>("getOriginalJson");
            signature = purchase.Call<string>("getSignature");
            purchaseToken = purchase.Call<string>("getPurchaseToken");

            var skuDetailsJson = skuDetails.Select(skuDetail => skuDetail.Call<string>("getOriginalJson")).ToList();
            receipt = GoogleReceiptEncoder.EncodeReceipt(
                originalJson,
                signature,
                skuDetailsJson
            );
        }

        public virtual bool IsAcknowledged()
        {
            return javaPurchase != null && javaPurchase.Call<bool>("isAcknowledged");
        }

        public virtual bool IsPurchased()
        {
            return javaPurchase != null && purchaseState == GooglePurchaseStateEnum.Purchased();
        }

        public virtual bool IsPending()
        {
            return javaPurchase != null && purchaseState == GooglePurchaseStateEnum.Pending();
        }
    }
}
