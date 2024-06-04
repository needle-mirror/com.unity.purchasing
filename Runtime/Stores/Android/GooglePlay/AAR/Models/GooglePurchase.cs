#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.MiniJSON;
using UnityEngine.Purchasing.Utils;

namespace UnityEngine.Purchasing.Models
{
    /// <summary>
    /// This is C# representation of the Java Class Purchase
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase">See more</a>
    /// </summary>
    class GooglePurchase : IGooglePurchase
    {
        public bool isAcknowledged { get; }
        public int purchaseState { get; }
        public List<string> skus { get; }
        public string orderId { get; }
        public string receipt { get; }
        public string signature { get; }
        public string originalJson { get; }
        public string purchaseToken { get; }
        public string? obfuscatedAccountId { get; }
        public string obfuscatedProfileId { get; }

        public string? sku => skus.FirstOrDefault();

        internal GooglePurchase(AndroidJavaObject purchase, IEnumerable<AndroidJavaObject> productDetailsEnum)
        {
            using var skusList = purchase.Call<AndroidJavaObject>("getProducts");

            isAcknowledged = purchase.Call<bool>("isAcknowledged");
            purchaseState = purchase.Call<int>("getPurchaseState");
            skus = skusList.Enumerate<string>().ToList();
            orderId = purchase.Call<string>("getOrderId");
            originalJson = purchase.Call<string>("getOriginalJson");
            signature = purchase.Call<string>("getSignature");
            purchaseToken = purchase.Call<string>("getPurchaseToken");
            var accountIdentifiers = purchase.Call<AndroidJavaObject>("getAccountIdentifiers");
            obfuscatedAccountId = accountIdentifiers.Call<string>("getObfuscatedAccountId");
            obfuscatedProfileId = accountIdentifiers.Call<string>("getObfuscatedProfileId");

            var productDetailsJson = productDetailsEnum.Select(productDetails => ProductDetailsConverter.BuildProductDescription(productDetails).metadata.GetGoogleProductMetadata().originalJson).ToList();
            receipt = GoogleReceiptEncoder.EncodeReceipt(
                originalJson,
                signature,
                productDetailsJson
            );
        }

        public virtual bool IsAcknowledged() => isAcknowledged;

        public virtual bool IsPurchased() => purchaseState == GooglePurchaseStateEnum.Purchased();

        public virtual bool IsPending() => purchaseState == GooglePurchaseStateEnum.Pending();
    }
}
