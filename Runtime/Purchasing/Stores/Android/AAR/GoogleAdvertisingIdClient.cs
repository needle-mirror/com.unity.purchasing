#nullable enable
using System;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    [Preserve]
    internal class GoogleAdvertisingIdClient : IGoogleAdvertisingIdClient
    {
        const string k_AdvertisingIdClientClass = "com.google.android.gms.ads.identifier.AdvertisingIdClient";
        static AndroidJavaClass? s_AdsInfoClass;

        static AndroidJavaClass GetAdsInfoClass()
        {
            s_AdsInfoClass ??= new AndroidJavaClass(k_AdvertisingIdClientClass);
            return s_AdsInfoClass;
        }

        public string? FetchGaid()
        {
            try
            {
                using var activity = UnityActivity.GetCurrentActivity();
                using var adInfo = GetAdsInfoClass()
                    .CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", activity);
                return adInfo.Call<string>("getId");
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
