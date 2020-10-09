using System;
using UnityEngine.Purchasing;

namespace UnityEngine.Purchasing
{
	public interface IMicrosoftExtensions : IStoreExtension
	{
		void RestoreTransactions();
	}
}
