using System;

namespace UnityEngine.Purchasing
{
    class UnityActivity
    {
        const string k_AndroidClassName = "com.unity3d.player.UnityPlayer";
        static AndroidJavaClass s_UnityPlayerClass;
        static AndroidJavaClass GetUnityPlayerClass()
        {
            s_UnityPlayerClass ??= new AndroidJavaClass(k_AndroidClassName);
            return s_UnityPlayerClass;
        }

        internal static AndroidJavaObject GetCurrentActivity()
        {
            return GetUnityPlayerClass().GetStatic<AndroidJavaObject>("currentActivity");
        }
    }
}
