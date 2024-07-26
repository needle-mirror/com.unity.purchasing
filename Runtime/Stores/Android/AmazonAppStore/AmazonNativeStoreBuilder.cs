#nullable enable

using Uniject;
using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    class AmazonNativeStoreBuilder : IAmazonNativeStoreFactory
    {
        const string k_AmazonPurchasingClassName = "com.unity.purchasing.amazon.AmazonPurchasing";

        public IAmazonJavaStore GetAmazonStore(IUnityCallback callback, IUtil util)
        {
            using var pluginClass = new AndroidJavaClass(k_AmazonPurchasingClassName);

            // Switch Android callbacks to the scripting thread, via ScriptingUnityCallback.
            var proxy = new JavaBridge(new ScriptingUnityCallback(callback, util));
            var instance = pluginClass.CallStatic<AndroidJavaObject>("instance", proxy);
            return new AmazonJavaStore(instance);
        }
    }
}
