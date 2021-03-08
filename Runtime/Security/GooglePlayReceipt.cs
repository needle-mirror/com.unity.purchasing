using System;

namespace UnityEngine.Purchasing.Security
{
	// See Google's reference docs.
	// http://developer.android.com/google/play/billing/billing_reference.html
	public enum GooglePurchaseState {
		Purchased,
		Cancelled,
		Refunded
	}

	public class GooglePlayReceipt : IPurchaseReceipt
	{
		public string productID { get; private set; }
		public string orderID { get; private set; }
		[Obsolete("Use orderID")]
		public string transactionID { get; private set; }
		public string packageName { get; private set; }
		public string purchaseToken { get; private set; }
		public DateTime purchaseDate { get; private set; }
		public GooglePurchaseState purchaseState { get; private set; }

		public GooglePlayReceipt(string productID, string transactionID, string orderID, string packageName,
			string purchaseToken, DateTime purchaseTime, GooglePurchaseState purchaseState) {
			this.productID = productID;
#pragma warning disable 618
            this.transactionID = transactionID;
#pragma warning restore 618
            this.orderID = orderID;
			this.packageName = packageName;
			this.purchaseToken = purchaseToken;
			this.purchaseDate = purchaseTime;
			this.purchaseState = purchaseState;
		}

		[Obsolete("Use variant with string orderID in signature")]
		public GooglePlayReceipt(string productID, string transactionID, string packageName,
			string purchaseToken, DateTime purchaseTime, GooglePurchaseState purchaseState) :
			this(productID, transactionID, transactionID, packageName, purchaseToken, purchaseTime, purchaseState) {
		}
	}
}
