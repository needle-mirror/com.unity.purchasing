using System;

namespace UnityEngine.Purchasing.Security
{
	/// <summary>
	/// An Apple receipt as defined here:
	/// https://developer.apple.com/library/ios/releasenotes/General/ValidateAppStoreReceipt/Chapters/ReceiptFields.html#//apple_ref/doc/uid/TP40010573-CH106-SW1
	/// </summary>
	public class AppleReceipt
	{
		public string bundleID { get; internal set; }
		public string appVersion { get; internal set; }
		public DateTime expirationDate { get; internal set; }
		public byte[] opaque { get; internal set; }
		public byte[] hash { get; internal set; }
		public string originalApplicationVersion { get; internal set; }
		public DateTime receiptCreationDate { get; internal set; }
		public AppleInAppPurchaseReceipt[] inAppPurchaseReceipts;
	}

	/// <summary>
	/// The details of an individual purchase.
	/// </summary>
	public class AppleInAppPurchaseReceipt : IPurchaseReceipt {
		public int quantity { get; internal set; }
		public string productID { get; internal set; }
		public string transactionID { get; internal set; }
		public string originalTransactionIdentifier { get; internal set; }
		public DateTime purchaseDate { get; internal set; }
		public DateTime originalPurchaseDate { get; internal set; }
		public DateTime subscriptionExpirationDate { get; internal set; }
		public DateTime cancellationDate { get; internal set; }
        public int isFreeTrial { get; internal set; }
        public int productType { get; internal set; }
        public int isIntroductoryPricePeriod {get; internal set; }
	}
}

