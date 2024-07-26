#nullable enable

using System;
using Uniject;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    [Obsolete]
    internal class NativeStoreProvider : INativeStoreProvider
    {
        readonly IAmazonNativeStoreFactory m_AmazonNativeStoreFactory;


        public NativeStoreProvider(IAmazonNativeStoreFactory amazonNativeStoreFactory)
        {
            m_AmazonNativeStoreFactory = amazonNativeStoreFactory;
        }

        public IAndroidJavaStore GetAmazonStore(IUnityCallback callback, IUtil util)
        {
            return m_AmazonNativeStoreFactory.GetAmazonStore(callback, util);
        }

        public INativeAppleStore GetStorekit(IUnityCallback callback)
        {
            // Both tvOS, iOS and visionOS use the same Objective-C linked to the XCode project.
            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.tvOS
#if UNITY_VISIONOS
                || Application.platform == RuntimePlatform.VisionOS
#endif
               )
            {
                return new iOSStoreBindings();
            }
            return new OSXStoreBindings();
        }
    }
}
