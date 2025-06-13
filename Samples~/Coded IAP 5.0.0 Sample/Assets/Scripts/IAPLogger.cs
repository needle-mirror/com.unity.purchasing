using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using UnityEngine.UI;

namespace Samples.Purchasing.IAP5.Demo
{
    public class IAPLogger : MonoBehaviour
    {
        public Text inAppConsole;
        public void LogFetchedProducts(List<Product> products)
        {
            if (products.Count > 0)
            {
                foreach (var product in products)
                {
                    LogConsole($"Fetched {product.definition.id}");
                }
            }
            else
            {
                LogConsole("No Products Fetched.");
            }
        }
        public void LogConfirmedOrder(Product product, IOrderInfo orderInfo)
        {
            LogConsole("===========");
            LogConsole($"Confirmed Product: '{product.definition.id}'");
            LogConsole($"Product transaction id: {orderInfo.TransactionID}.");
            LogConsole($"Product receipt length: {orderInfo.Receipt?.Length}.");
            LogConsole($"Product Type: '{product.definition.type}'");
        }
        public void LogReceiptValidation(IPurchaseReceipt productReceipt)
        {
            LogConsole($"Product ID: '{productReceipt.productID}', Date: '{productReceipt.purchaseDate}', Transaction ID: '{productReceipt.transactionID}'");
            LogGooglePlayReceiptValidationInfo(productReceipt);
            LogAppleReceiptValidationInfo(productReceipt);
        }
        public void LogGooglePlayReceiptValidationInfo(IPurchaseReceipt productReceipt)
        {
            GooglePlayReceipt googleReceipt = productReceipt as GooglePlayReceipt;
            if (googleReceipt != null)
            {
                LogConsole($"GooglePlay - State: '{googleReceipt.purchaseState}', Token: '{googleReceipt.purchaseToken}'");
            }
        }
        public void LogAppleReceiptValidationInfo(IPurchaseReceipt productReceipt)
        {
            AppleInAppPurchaseReceipt appleReceipt = productReceipt as AppleInAppPurchaseReceipt;
            if (appleReceipt != null)
            {
                LogConsole($"Apple - Original Transaction: '{appleReceipt.originalTransactionIdentifier}', Expiration Date : '{appleReceipt.subscriptionExpirationDate}', Cancellation Date : '{appleReceipt.cancellationDate}', Quandtity : '{appleReceipt.quantity}'");
            }
        }
        public void LogCompletedPurchase(Product product, IOrderInfo orderInfo)
        {
            LogConsole("===========");
            LogConsole($"Purchased Product: '{product.definition.id}'");
            LogConsole($"Product transaction id: {orderInfo.TransactionID}.");
            LogConsole($"Product receipt length: {orderInfo.Receipt?.Length}.");
            LogConsole($"Product Type: '{product.definition.type}'");
        }

        public void LogFailedConfirmation(Product product, PurchaseFailureReason reason)
        {
            LogConsole("===========");
            LogConsole("Purchase Confirmation Failed");
            LogConsole($"Product: '{product.definition.storeSpecificId}'");
            LogConsole($"FailureReason: {reason.ToString()}.");
        }
        public void LogFailedPurchase(Product product, PurchaseFailureReason reason)
        {
            LogConsole("===========");
            LogConsole("PurchaseFailed");
            LogConsole($"Product: '{product.definition.storeSpecificId}'");
            LogConsole($"FailureReason: {reason.ToString()}.");
        }
        public void LogDeferredPurchase(Product product)
        {
            LogConsole("===========");
            LogConsole("PurchaseDeferred");
            LogConsole($"Product: '{product.definition.storeSpecificId}'");
        }
        public void LogConsole(string msg)
        {
            Debug.Log(msg);
            if (inAppConsole.text.Length > 0)
            {
                inAppConsole.text = "\n" + inAppConsole.text;
            }
            inAppConsole.text = msg + inAppConsole.text;
        }
    }
}
