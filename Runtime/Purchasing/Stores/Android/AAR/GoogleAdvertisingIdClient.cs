#nullable enable
using System;
using System.Threading.Tasks;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    [Preserve]
    internal class GoogleAdvertisingIdClient : IGoogleAdvertisingIdClient
    {
        const string k_AdvertisingIdClientClass = "com.google.android.gms.ads.identifier.AdvertisingIdClient";
        static AndroidJavaClass? s_AdsInfoClass;
        // null = not probed yet; false is remembered for the session.
        static bool? s_ClassAvailable;

        static AndroidJavaClass? GetAdsInfoClass()
        {
            if (s_ClassAvailable == false)
            {
                return null;
            }

            try
            {
                s_AdsInfoClass ??= new AndroidJavaClass(k_AdvertisingIdClientClass);
                s_ClassAvailable = true;
                return s_AdsInfoClass;
            }
            catch (Exception)
            {
                s_ClassAvailable = false;
                return null;
            }
        }

        public async Task<string?> FetchGaidAsync()
        {
            if (s_ClassAvailable == false)
            {
                return null;
            }

            try
            {
                return await Task.Run(() =>
                {
                    AndroidJNI.AttachCurrentThread();
                    try
                    {
                        var adsInfoClass = GetAdsInfoClass();
                        if (adsInfoClass == null)
                        {
                            return null;
                        }

                        using var activity = UnityActivity.GetCurrentActivity();
                        using var adInfo = adsInfoClass
                            .CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", activity);
                        return adInfo.Call<string>("getId");
                    }
                    finally
                    {
                        AndroidJNI.DetachCurrentThread();
                    }
                });
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
