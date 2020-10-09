using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
	public interface ISamsungAppsConfiguration : IStoreConfiguration
	{
		void SetMode(SamsungAppsMode mode);
	}
}
