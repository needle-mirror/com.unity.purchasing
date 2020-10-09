#define UNITY_UNIFIED_IAP

using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Purchasing;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor.Purchasing
{

    [InitializeOnLoad]
    public static class FacebookBuildControl
    {
#if !UNITY_UNIFIED_IAP
        private const string BinPath = "Assets/Plugins/UnityPurchasing/Bin/Facebook/";
#else
        private const string k_FacebookDefineSymbol = "UNITY_PURCHASING_FACEBOOK";
#endif

        private const string minVersion = "7.9.4.0";

#if !UNITY_UNIFIED_IAP
        private static PluginImporter stubImporter;
        private static PluginImporter liveImporter;
#endif

#if UNITY_2017_1_OR_NEWER
        class BuildTargetChangedHandler : Build.IActiveBuildTargetChanged
        {
            public int callbackOrder { get { return 0; } }

            public void OnActiveBuildTargetChanged(BuildTarget oldTarget, BuildTarget newTarget)
            {
                FacebookBuildControl.SetupFacebookConfig();
            }
        }
#endif

        static FacebookBuildControl()
        {
            Console.WriteLine("UnityIAP: [InitializeOnLoad] Facebook Check");
            Console.WriteLine("UnityIAP: Runtime [{0}]", Application.unityVersion);

#if !UNITY_UNIFIED_IAP
            stubImporter = (PluginImporter)PluginImporter.GetAtPath(BinPath + "FacebookStore.dll");
            liveImporter = (PluginImporter)PluginImporter.GetAtPath(BinPath + "live/FacebookStore.dll");
#endif

#if !UNITY_2017_1_OR_NEWER
            EditorUserBuildSettings.activeBuildTargetChanged += SetupFacebookConfig;
#endif

            // Go ahead and run once on load
            SetupFacebookConfig();
        }


        public static void SetupFacebookConfig()
        {
            // Automatic switching doesn't behave nicely prior to 5.5 and Facebook Gameroom
            // isn't officially supported by Unity IAP in older releases so we'll just skip the init
            if( (VersionCheck.LessThan(Application.unityVersion, "5.5.0")) )
                return;

            Console.WriteLine("UnityIAP: Current Build is {1}:{0}",
                EditorUserBuildSettings.activeBuildTarget,
                EditorUserBuildSettings.selectedBuildTargetGroup);

            // This appears to be the most reliable approach that covers both 5.6 and the 5.5 FB betas
            if(BuildTargetGroup.IsDefined(typeof(BuildTargetGroup), "Facebook"))
            {
                if(EditorUserBuildSettings.selectedBuildTargetGroup == (BuildTargetGroup)Enum.Parse(typeof(BuildTargetGroup), "Facebook"))
                {
                    // Console.WriteLine("UnityIAP: Facebook BuildTargetGroup is Selected ({1}:{0})",
                    //     EditorUserBuildSettings.activeBuildTarget,
                    //     EditorUserBuildSettings.selectedBuildTargetGroup);

                    // Apparently we need to allow for occasional switching on initial click on Facebook
                    // "platform" in Build As... This covers that situation
                    if ( (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL) ||
                         (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows) ||
                         (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64) )
                    {
                        Console.WriteLine("UnityIAP: Valid Facebook Gameroom Target");
                        EnableFacebookIAP();
                        return;
                    }
                }
                else
                {
                    if ( (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL) )
                    {
                        Console.WriteLine("UnityIAP: Possible Facebook Canvas Target");
                        EnableFacebookIAP();
                        return;
                    }
                }
            }
            DisableFacebookIAP();
            return;
        }


        static bool EnableFacebookIAP()
        {
#if !UNITY_UNIFIED_IAP
            if( (liveImporter == null) || (stubImporter == null) ||
                (VersionCheck.LessThan(Application.unityVersion, "5.5.0")) )
                return false;
#endif

            if(IsFacebookAvailable())
            {
#if !UNITY_UNIFIED_IAP
                // This approach is the most reliable way to manage this
                // with the current BuildTargetGroup implementation in 5.6.
                // We may need to revisit for future releases

                stubImporter.ClearSettings(); // sets the stub library to Any (cleanly)
                stubImporter.SetCompatibleWithAnyPlatform(false); // then toggle
                stubImporter.SetCompatibleWithEditor(true);

                liveImporter.ClearSettings(); // sets the live library to Any (cleanly)
                liveImporter.SetExcludeEditorFromAnyPlatform(true);
#else
                BuildDefines.getScriptCompilationDefinesDelegates -= AddFacebookBuildDefines;
                BuildDefines.getScriptCompilationDefinesDelegates -= RemoveFacebookBuildDefines;
                BuildDefines.getScriptCompilationDefinesDelegates += AddFacebookBuildDefines;
#endif

                Console.WriteLine("UnityIAP: live/FacebookStore.dll enabled");
                return true;
            }

            Console.WriteLine("UnityIAP: could not enable Facebook IAP on eligible platform");
            return false;
        }

        static bool DisableFacebookIAP()
        {
#if !UNITY_UNIFIED_IAP
            // This approach requires that the "reference" meta files for the FacebookStore.dll stores
            // be set as "All" for the stub and "none" for the live version

            if( (liveImporter == null) || (stubImporter == null) ||
                (VersionCheck.LessThan(Application.unityVersion, "5.5.0")) )
                return false;

            stubImporter.ClearSettings(); // sets the stub library to Any (cleanly)

            liveImporter.ClearSettings(); // sets the live library to Any (cleanly)
            liveImporter.SetCompatibleWithAnyPlatform(false); // and then toggle off
#else
            BuildDefines.getScriptCompilationDefinesDelegates -= AddFacebookBuildDefines;
            BuildDefines.getScriptCompilationDefinesDelegates -= RemoveFacebookBuildDefines;
            BuildDefines.getScriptCompilationDefinesDelegates += RemoveFacebookBuildDefines;
#endif
            Console.WriteLine("UnityIAP: stub FacebookStore.dll disabled");
            return true;
        }


        //  Confirm there's a Facebook SDK Version that will actually work correctly...
        //
        static bool IsFacebookAvailable()
        {
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in loadedAssemblies)
            {
                if(assembly.GetName().Name == "Facebook.Unity")
                {
                    if(assembly.GetName().Version >= new Version(minVersion))
                    {
                        Console.WriteLine("UnityIAP FB: Compatible version of Facebook SDK Present");
                        return true;
                    }
                    else
                    {
                        Debug.LogError("UnityIAP: Older version of Facebook SDK (<b>" + assembly.GetName().Version +
                                       "</b>) is not compatible with Unity IAP, please update in Player Settings");
                    }
                }
            }
            Console.WriteLine("UnityIAP FB: No Facebook SDK Present");
            return false;
        }

#if UNITY_UNIFIED_IAP
        private static void AddFacebookBuildDefines(BuildTarget target, HashSet<string> defines)
        {
            defines.Add(k_FacebookDefineSymbol);
        }

        private static void RemoveFacebookBuildDefines(BuildTarget target, HashSet<string> defines)
        {
            defines.Remove(k_FacebookDefineSymbol);
        }
#endif
    }
}
