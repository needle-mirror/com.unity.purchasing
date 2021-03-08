using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
	public interface IAmazonExtensions : IStoreExtension
	{
		string amazonUserId { get; }

		// Amazon makes it possible to notify them of a
		// product that cannot be fulfilled.
		// This method calls Amazon's
		// notifyFulfillment(transactionID, FulfillmentResult.UNAVAILABLE);
		// https://developer.amazon.com/public/apis/earn/in-app-purchasing/docs-v2/implementing-iap-2.0
		void NotifyUnableToFulfillUnavailableProduct(string transactionID);
	}
}
