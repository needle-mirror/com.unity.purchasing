using System;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
	/// <summary>
	/// Access Samsung Apps specific functionality.
	/// </summary>
	public interface ISamsungAppsExtensions : IStoreExtension
	{
		void RestoreTransactions(Action<bool> callback);
	}
}
