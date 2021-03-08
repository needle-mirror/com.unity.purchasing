using System;

namespace UnityEngine.Purchasing.Security
{
	/// <summary>
	/// Represents a parsed purchase receipt from a store.
	/// </summary>
	public interface IPurchaseReceipt
	{
		string transactionID { get; }
		string productID { get; }
		DateTime purchaseDate { get; }
	}
}
