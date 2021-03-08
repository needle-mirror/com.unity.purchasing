using System;

namespace UnityEngine.Purchasing
{
	public class FakeSamsungAppsExtensions : ISamsungAppsExtensions, ISamsungAppsConfiguration
	{
		public void SetMode(SamsungAppsMode mode)
		{           
		}

		public void RestoreTransactions(Action<bool> callback)
		{
			callback(true);
		}
	}
}

