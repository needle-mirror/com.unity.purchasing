using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace UnityEngine.Purchasing
{
	public class FakeAmazonExtensions : IAmazonExtensions, IAmazonConfiguration
	{
		public void WriteSandboxJSON(HashSet<ProductDefinition> products)
		{
		}

		public void NotifyUnableToFulfillUnavailableProduct(string transactionID) {
		}

		public string amazonUserId
		{
			get { return "fakeid"; }
		}
	}
}
