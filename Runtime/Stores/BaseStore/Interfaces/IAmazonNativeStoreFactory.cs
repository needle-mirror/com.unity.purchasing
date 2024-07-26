#nullable enable

using Uniject;
using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    interface IAmazonNativeStoreFactory
    {
        IAmazonJavaStore GetAmazonStore(IUnityCallback callback, IUtil util);
    }
}
