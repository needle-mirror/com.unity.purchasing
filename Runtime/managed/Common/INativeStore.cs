using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// An interface to native underlying store systems. Provides a base for opaquely typed
    /// communication across a language-bridge upon which additional functionality can be composed. 
    /// Is used by most public IStore implementations which themselves are owned by the purchasing 
    /// core.
    /// </summary>
	public interface INativeStore
	{
		void RetrieveProducts(String json);
		void Purchase(string productJSON, string developerPayload);
		void FinishTransaction(string productJSON, string transactionID);
	}

	internal delegate void UnityPurchasingCallback(string subject, string payload, string receipt, string transactionId);
}
