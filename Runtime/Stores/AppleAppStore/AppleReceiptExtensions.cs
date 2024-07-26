#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Security;

namespace UnityEngine.Purchasing
{
    static class AppleReceiptExtensions
    {
        public static bool HasInAppPurchaseReceipts(this AppleReceipt? appleReceipt)
        {
            return appleReceipt?.inAppPurchaseReceipts?.Length > 0;
        }

        public static AppleInAppPurchaseReceipt? FindMostRecentReceiptForProduct(this AppleReceipt appleReceipt,
            string productId)
        {
            var foundReceipts = Array.FindAll(appleReceipt.inAppPurchaseReceipts, (r) => r.productID == productId);
            Array.Sort(foundReceipts, (b, a) => a.purchaseDate.CompareTo(b.purchaseDate));
            return FirstNonCancelledReceipt(foundReceipts);
        }

        static AppleInAppPurchaseReceipt? FirstNonCancelledReceipt(IEnumerable<AppleInAppPurchaseReceipt> receipts)
        {
            return receipts.FirstOrDefault(receipt => !receipt.IsCancelled());
        }

        static bool IsCancelled(this AppleInAppPurchaseReceipt receipt)
        {
            return receipt.cancellationDate != DateTime.MinValue;
        }
    }
}
