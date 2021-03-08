using System.Collections.Generic;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
	public interface IAmazonConfiguration : IStoreConfiguration
	{
		void WriteSandboxJSON(HashSet<ProductDefinition> products);
	}
}
