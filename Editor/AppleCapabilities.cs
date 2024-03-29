#if UNITY_TVOS || UNITY_IOS || UNITY_VISIONOS
using System;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace UnityEditor.Purchasing
{
    class AppleCapabilities : IPostprocessBuildWithReport
    {
        const string k_StorekitFramework = "StoreKit.framework";
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.tvOS || report.summary.platform == BuildTarget.iOS
#if UNITY_VISIONOS
                || report.summary.platform == BuildTarget.VisionOS
#endif
                )
            {
                OnPostprocessBuild(report.summary.platform, report.summary.outputPath);
            }
        }

        static void OnPostprocessBuild(BuildTarget buildTarget, string path)
        {
            OnPostprocessBuildForApple(path);
        }

        static void OnPostprocessBuildForApple(string path)
        {
#if UNITY_IOS || UNITY_TVOS
            var projPath = PBXProject.GetPBXProjectPath(path);
#elif UNITY_VISIONOS
            var projPath = Path.Combine(path, "Unity-VisionOS.xcodeproj/project.pbxproj");
#endif
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);

            AddStoreKitFramework(proj, projPath);
            AddInAppPurchasingCapability(projPath, proj);
        }

        static void AddInAppPurchasingCapability(string projPath, PBXProject proj)
        {
            var manager = new ProjectCapabilityManager(
                projPath,
                null,
                targetGuid: proj.GetUnityMainTargetGuid()
            );
            manager.AddInAppPurchase();
            manager.WriteToFile();
        }

        static void AddStoreKitFramework(PBXProject proj, string projPath)
        {
            foreach (var targetGuid in new[] { proj.GetUnityMainTargetGuid(), proj.GetUnityFrameworkTargetGuid() })
            {
                proj.AddFrameworkToProject(targetGuid, k_StorekitFramework, false);
                System.IO.File.WriteAllText(projPath, proj.WriteToString());
            }
        }
    }
}
#endif
