using System;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.Purchasing;
using UnityEngine;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal class PurchasingImporter : IActiveBuildTargetChanged
    {
        static PurchasingImporter()
        {
            PurchasingSettings.ApplyEnableSettings(EditorUserBuildSettings.activeBuildTarget);
            CheckDependency(EditorUserBuildSettings.activeBuildTarget);
        }

        public int callbackOrder => 0;

        private const string k_GdkPackage = "com.unity.microsoft.gdk";
        private const string k_GdkToolsPackage = "com.unity.microsoft.gdk.tools";
        private const string k_InstallDecisionKey = "IAP_GDKPackageInstall";

        private static ListRequest s_PackageListRequest;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnLoad()
        {
            s_PackageListRequest = null;
        }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            CheckDependency(newTarget);
        }

        private static void CheckDependency(BuildTarget buildTarget)
        {
            if (!Application.isBatchMode
                && s_PackageListRequest == null
                && (buildTarget == BuildTarget.StandaloneWindows64 || buildTarget == BuildTarget.GameCoreXboxSeries || buildTarget == BuildTarget.GameCoreXboxOne))
            {
                s_PackageListRequest = Client.List();
                EditorApplication.update += Update;
            }
        }

        private static void Update()
        {
            if (s_PackageListRequest != null && s_PackageListRequest.IsCompleted)
            {
                EditorApplication.update -= Update;
                if (s_PackageListRequest.Status == StatusCode.Success)
                {
                    var gdkPackage = s_PackageListRequest.Result.FirstOrDefault(q => q.name == k_GdkPackage);
                    var gdkToolsPackage = s_PackageListRequest.Result.FirstOrDefault(q => q.name == k_GdkToolsPackage);
                    if (gdkPackage == null || gdkToolsPackage == null)
                    {
                        PromptGdkPackageInstall();
                    }
                }
                s_PackageListRequest = null;
            }
            else if (s_PackageListRequest == null)
            {
                EditorApplication.update -= Update;
            }
        }

        private static void PromptGdkPackageInstall()
        {
            var promptOptOut = EditorPrefs.GetBool(k_InstallDecisionKey, false);
            if (!promptOptOut)
            {
                var title = "Required Dependency Missing";
                var message = $"The In-App Purchasing package requires the Microsoft GDK package in order to support the Xbox Store on Windows or Xbox.\n" +
                    $"Do you want to install the Microsoft GDK package? The following package will be installed:" +
                    $"\n{k_GdkPackage}\n{k_GdkToolsPackage}\n\n" +
                    "If these packages are not installed, the In-App Purchasing package will use FakeStore as its store.";
                var okString = "Install";
                var cancelString = "Don't install";
#if UNITY_6000_3_OR_NEWER
                var consentToInstall = EditorDialog.DisplayDecisionDialog(title, message, okString, cancelString);
#else
                var consentToInstall = EditorUtility.DisplayDialog(title, message, okString, cancelString);
#endif
                if (consentToInstall)
                {
                    Client.Add(k_GdkPackage);
                    Client.Add(k_GdkToolsPackage);
                }
                // Never ask again
                EditorPrefs.SetBool(k_InstallDecisionKey, true);
            }
            else
            {
                Debug.Log("The Microsoft GDK package is not installed. The selected store is FakeStore.");
            }
        }
    }
}
