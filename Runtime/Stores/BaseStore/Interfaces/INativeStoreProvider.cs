#nullable enable

using System;
using Uniject;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    internal interface INativeStoreProvider
    {

        IAndroidJavaStore GetAmazonStore(IUnityCallback callback, IUtil util);
        INativeAppleStore GetStorekit(IUnityCallback callback);
    }
}
