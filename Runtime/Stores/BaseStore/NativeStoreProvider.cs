#nullable enable

namespace UnityEngine.Purchasing
{
    internal class NativeStoreProvider : INativeStoreProvider
    {
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
