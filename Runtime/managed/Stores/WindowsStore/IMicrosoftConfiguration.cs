using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
	public interface IMicrosoftConfiguration : IStoreConfiguration
	{
		bool useMockBillingSystem { get; set; }
	}
}
