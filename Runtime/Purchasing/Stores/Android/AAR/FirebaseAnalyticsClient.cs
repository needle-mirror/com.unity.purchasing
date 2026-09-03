#nullable enable
using System;
using System.Threading.Tasks;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    [Preserve]
    internal class FirebaseAnalyticsClient : IFirebaseAnalyticsClient
    {
        const string k_FirebaseAnalyticsClass = "com.google.firebase.analytics.FirebaseAnalytics";
        const string k_TasksClass = "com.google.android.gms.tasks.Tasks";
        const string k_FirebaseAppClass = "com.google.firebase.FirebaseApp";

        static AndroidJavaClass? s_AnalyticsClass;
        static AndroidJavaClass? s_TasksClass;
        static AndroidJavaClass? s_FirebaseAppClass;
        // null = not probed yet; false is remembered for the session.
        static bool? s_FirebaseAvailable;

        static AndroidJavaClass GetAnalyticsClass()
        {
            s_AnalyticsClass ??= new AndroidJavaClass(k_FirebaseAnalyticsClass);
            return s_AnalyticsClass;
        }

        static AndroidJavaClass GetTasksClass()
        {
            s_TasksClass ??= new AndroidJavaClass(k_TasksClass);
            return s_TasksClass;
        }

        static AndroidJavaClass GetFirebaseAppClass()
        {
            s_FirebaseAppClass ??= new AndroidJavaClass(k_FirebaseAppClass);
            return s_FirebaseAppClass;
        }

        static bool FirebaseAvailable()
        {
            if (s_FirebaseAvailable.HasValue)
            {
                return s_FirebaseAvailable.Value;
            }

            try
            {
                GetAnalyticsClass();
                GetFirebaseAppClass();
                s_FirebaseAvailable = true;
                return true;
            }
            catch (Exception)
            {
                s_FirebaseAvailable = false;
                return false;
            }
        }

        public async Task<string?> FetchAppInstanceIdAsync()
        {
            if (!FirebaseAvailable())
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
                        using var activity = UnityActivity.GetCurrentActivity();
                        using var analytics = GetAnalyticsClass().CallStatic<AndroidJavaObject>("getInstance", activity);
                        using var task = analytics.Call<AndroidJavaObject>("getAppInstanceId");
                        using var result = GetTasksClass().CallStatic<AndroidJavaObject>("await", task);
                        return result.Call<string>("toString");
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

        public async Task<string?> FetchSessionIdAsync()
        {
            if (!FirebaseAvailable())
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
                        using var activity = UnityActivity.GetCurrentActivity();
                        using var analytics = GetAnalyticsClass().CallStatic<AndroidJavaObject>("getInstance", activity);
                        using var task = analytics.Call<AndroidJavaObject>("getSessionId");
                        using var result = GetTasksClass().CallStatic<AndroidJavaObject>("await", task);
                        return result.Call<string>("toString");
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

        public async Task<string?> FetchAppIdAsync()
        {
            if (!FirebaseAvailable())
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
                        using var app = GetFirebaseAppClass().CallStatic<AndroidJavaObject>("getInstance");
                        using var options = app.Call<AndroidJavaObject>("getOptions");
                        return options.Call<string>("getApplicationId");
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
