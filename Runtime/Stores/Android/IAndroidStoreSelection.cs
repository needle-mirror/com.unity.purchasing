using System;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
	public interface IAndroidStoreSelection : IStoreConfiguration
	{
	    AndroidStore androidStore { get; }
	    AppStore appStore { get; }
	}
}
