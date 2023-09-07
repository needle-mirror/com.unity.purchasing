using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Callbacks;
using UnityEditor.Connect;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// Editor tools to set build-time configurations for app stores.
    /// </summary>
    [InitializeOnLoad]
    public static class UnityPurchasingEditor
    {
        const string PurchasingPackageName = "com.unity.purchasing";

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        internal const string UdpPackageName = "com.unity.purchasing.udp";

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        const string k_UdpErrorText = "In order to use UDP functionality, you must install or update the Unity Distribution Portal Package. Please configure your project's packages before running UDP-related editor commands in batch mode.";

        const string ModePath = "Assets/Resources/BillingMode.json";
        const string prevModePath = "Assets/Plugins/UnityPurchasing/Resources/BillingMode.json";

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static ListRequest m_ListRequestOfDependentPackages;

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static SearchRequest m_SearchRequestOfAvailablePackages;

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static bool m_UdpUpmPackageInstalled;

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static bool m_UdpUpmPackageAvailable;

        const string BinPath = "Packages/com.unity.purchasing/Plugins/UnityPurchasing/Android";

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        const string AssetStoreUdpBinPath = "Assets/Plugins/UDP/Android";

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static readonly string PackManUdpBinPath = $"Packages/{UdpPackageName}/Android";

        static StoreConfiguration config;
        static readonly AppStore defaultAppStore = AppStore.GooglePlay;
        internal delegate void AndroidTargetChange(AppStore store);
        internal static AndroidTargetChange OnAndroidTargetChange;

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static readonly bool s_udpAvailable = UdpSynchronizationApi.CheckUdpAvailability();

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        internal static bool IsUdpUpmPackageInstalled()
        {
            return m_UdpUpmPackageInstalled || File.Exists($"Packages/{UdpPackageName}/package.json");
        }

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static void ListingCurrentPackageProgress()
        {
            if (m_ListRequestOfDependentPackages.IsCompleted)
            {
                m_UdpUpmPackageInstalled = false;
                EditorApplication.update -= ListingCurrentPackageProgress;
                if (m_ListRequestOfDependentPackages.Status == StatusCode.Success)
                {
                    var udpPackage = m_ListRequestOfDependentPackages.Result.FirstOrDefault(package => package.name == UdpPackageName);

                    m_UdpUpmPackageInstalled = udpPackage != null;
                }
                else if (m_ListRequestOfDependentPackages.Status >= StatusCode.Failure)
                {
                    Debug.LogError(m_ListRequestOfDependentPackages.Error.message);
                }

                if (!m_UdpUpmPackageInstalled)
                {
                    CheckUdpUpmPackageAvailableViaPackageManager();
                }
            }
        }

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static void SearchingAvailablePackageProgress()
        {
            if (m_SearchRequestOfAvailablePackages.IsCompleted)
            {
                m_UdpUpmPackageAvailable = false;
                EditorApplication.update -= SearchingAvailablePackageProgress;
                if (m_SearchRequestOfAvailablePackages.Status == StatusCode.Success)
                {
                    var udpPackage = m_SearchRequestOfAvailablePackages.Result.FirstOrDefault(package => package.name == UdpPackageName);

                    m_UdpUpmPackageAvailable = udpPackage != null;
                }
                else if (m_SearchRequestOfAvailablePackages.Status >= StatusCode.Failure)
                {
                    Debug.LogError(m_SearchRequestOfAvailablePackages.Error.message);
                }
            }
        }

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        internal static bool IsUdpAssetStorePackageInstalled()
        {
            return File.Exists("Assets/UDP/UDP.dll") || File.Exists("Assets/Plugins/UDP/UDP.dll");
        }

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        [InitializeOnLoadMethod]
        static void CheckUdpUpmPackageInstalled()
        {
            if (IsInBatchMode())
            {
                CheckUdpUpmPackageInstalledViaManifest();
            }
            else
            {
                CheckUdpUpmPackageInstalledViaPackageManager();
            }
        }


        static bool IsInBatchMode()
        {
            return UnityEditorInternal.InternalEditorUtility.inBatchMode;
        }

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static void CheckUdpUpmPackageInstalledViaPackageManager()
        {
            if (IsInBatchMode())
            {
                Debug.unityLogger.LogIAPError("CheckUdpUmpPackageInstalledViaPackageManager will always fail in Batch Mode. Call CheckUdpUmpPackageInstalledViaManifest instead");
            }

            m_ListRequestOfDependentPackages = Client.List();
            EditorApplication.update += ListingCurrentPackageProgress;
        }

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static void CheckUdpUpmPackageInstalledViaManifest()
        {
            if (!IsInBatchMode())
            {
                Debug.unityLogger.LogIAPWarning("When not running in batch mode, it's more reliable to check the presence of UDP via CheckUdpUmpPackageInstalledViaPackageManager, in case the manifest file is out of date.");
            }

            m_UdpUpmPackageInstalled = false;

            if (File.Exists("Packages/manifest.json"))
            {
                var jsonText = File.ReadAllText("Packages/manifest.json");
                m_UdpUpmPackageInstalled = jsonText.Contains(UdpPackageName);
            }
        }

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static void CheckUdpUpmPackageAvailableViaPackageManager()
        {
            if (IsInBatchMode())
            {
                Debug.unityLogger.LogIAPError("CheckUdpUpmPackageAvailableViaPackageManager will always fail in Batch Mode.");
            }

            m_SearchRequestOfAvailablePackages = Client.SearchAll();
            EditorApplication.update += SearchingAvailablePackageProgress;
        }

        /// <summary>
        /// Since we are changing the billing mode's location, it may be necessary to migrate existing billing
        /// mode file to the new location.
        /// </summary>
        [InitializeOnLoadMethod]
        internal static void MigrateBillingMode()
        {
            try
            {
                var file = new FileInfo(ModePath);
                // This will create the new billing file location, if it already exists, this will not do anything.
                file.Directory.Create();

                // See if the file already exists in the new location.
                if (File.Exists(ModePath))
                {
                    return;
                }

                // check if the old exists before moving it
                if (DoesPrevModePathExist())
                {
                    AssetDatabase.MoveAsset(prevModePath, ModePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        internal static bool DoesPrevModePathExist()
        {
            return File.Exists(prevModePath);
        }

        // Notice: Multiple files per target supported. While Key must be unique, Value can be duplicated!
        static readonly Dictionary<string, AppStore> StoreSpecificFiles = new Dictionary<string, AppStore>()
        {
            {"billing-5.2.1.aar", AppStore.GooglePlay},
            {"AmazonAppStore.aar", AppStore.AmazonAppStore}
        };

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static readonly Dictionary<string, AppStore> UdpSpecificFiles = new Dictionary<string, AppStore>() {
            { "udp.aar", AppStore.UDP},
            { "udpsandbox.aar", AppStore.UDP},
            { "utils.aar", AppStore.UDP}
        };

        // Create or read BillingMode.json at Project Editor load
        static UnityPurchasingEditor()
        {
            EditorApplication.delayCall += () =>
            {
                if (File.Exists(ModePath))
                {
                    var oldAppStore = GetAppStoreSafe();
                    config = StoreConfiguration.Deserialize(File.ReadAllText(ModePath));
                    if (oldAppStore != config.androidStore)
                    {
                        OnAndroidTargetChange?.Invoke(config.androidStore);
                    }
                }
                else
                {
                    CreateDefaultBillingModeFile();
                }
            };
        }

        static void CreateDefaultBillingModeFile()
        {
            TargetAndroidStore(defaultAppStore);
        }

#if !ENABLE_EDITOR_GAME_SERVICES
        const string SwitchStoreMenuItem = IapMenuConsts.MenuItemRoot + "/Switch Store...";
        [MenuItem(SwitchStoreMenuItem, false, 200)]
        static void OnSwitchStoreMenu()
        {
            var window = EditorWindow.GetWindow(typeof(SwitchStoreEditorWindow));
            window.titleContent.text = IapMenuConsts.SwitchStoreTitleText;
            window.minSize = new Vector2(340, 180);
            window.Show();

            GameServicesEventSenderHelpers.SendTopMenuSwitchStoreEvent();
        }
#else
        const string SwitchStoreMenuItem = IapMenuConsts.MenuItemRoot + "/Configure...";
#endif

        private static AppStore GetAppStoreSafe()
        {
            var store = AppStore.NotSpecified;
            if (config != null)
            {
                store = config.androidStore;
            }

            return store;
        }

        /// <summary>
        /// Target a specified Android store.
        /// This sets the correct plugin importer settings for the store
        /// and writes the choice to BillingMode.json so the player
        /// can choose the correct store API at runtime.
        /// Note: This can fail if preconditions are not met for the AppStore.UDP target.
        /// </summary>
        /// <param name="target">App store to enable for next build</param>
        public static void TargetAndroidStore(AppStore target)
        {
            TryTargetAndroidStore(target);
        }

        internal static AppStore TryTargetAndroidStore(AppStore target)
        {
            if (!target.IsAndroid())
            {
                throw new ArgumentException(string.Format("AppStore parameter ({0}) must be an Android app store", target));
            }

            if (target == AppStore.UDP)
            {
                if (CheckAndHandleUdpUnavailability())
                {
                    return ConfiguredAppStore();
                }
            }

            ConfigureProject(target);
            SaveConfig(target);
            OnAndroidTargetChange?.Invoke(target);

            var targetString = Enum.GetName(typeof(AppStore), target);
            GenericEditorDropdownSelectEventSenderHelpers.SendIapMenuSelectTargetStoreEvent(targetString);

            return ConfiguredAppStore();
        }

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static bool CheckAndHandleUdpUnavailability()
        {
            if (!s_udpAvailable || (!IsUdpUpmPackageInstalled() && !IsUdpAssetStorePackageInstalled()) || !UdpSynchronizationApi.CheckUdpCompatibility())
            {
                if (IsInBatchMode())
                {
                    Debug.unityLogger.LogIAPError(k_UdpErrorText);
                }
                else
                {
                    if (m_UdpUpmPackageAvailable)
                    {
                        UdpInstaller.PromptUdpInstallation();
                    }
                    else
                    {
                        UdpInstaller.PromptUdpUnavailability();
                    }
                }

                return true;
            }

            return false;
        }

        // Unfortunately the UnityEditor API updates only the in-memory list of
        // files available to the build when what we want is a persistent modification
        // to the .meta files. So we must also rely upon the PostProcessScene attribute
        // below to process the
        private static void ConfigureProject(AppStore target)
        {
            foreach (var mapping in StoreSpecificFiles)
            {
                // All files enabled when store is determined at runtime.
                var enabled = target == AppStore.NotSpecified;
                // Otherwise this file must be needed on the target.
                enabled |= mapping.Value == target;

                var path = string.Format("{0}/{1}", BinPath, mapping.Key);
                var importer = (PluginImporter)AssetImporter.GetAtPath(path);

                if (importer != null)
                {
                    importer.SetCompatibleWithPlatform(BuildTarget.Android, enabled);
                }
                else
                {
                    // Search for any occurrence of this file
                    // Only fail if more than one found
                    var paths = FindPaths(mapping.Key);

                    if (paths.Length == 1)
                    {
                        importer = (PluginImporter)AssetImporter.GetAtPath(paths[0]);
                        importer.SetCompatibleWithPlatform(BuildTarget.Android, enabled);
                    }
                }
            }

            ConfigureProjectForUdp(target);
        }

        [Obsolete("Internal API to be removed with UDP deprecation.")]
        static void ConfigureProjectForUdp(AppStore target)
        {
            var UdpBinPath = IsUdpUpmPackageInstalled() ? PackManUdpBinPath :
                IsUdpAssetStorePackageInstalled() ? AssetStoreUdpBinPath :
                null;

            if (s_udpAvailable && !string.IsNullOrEmpty(UdpBinPath))
            {
                foreach (var mapping in UdpSpecificFiles)
                {
                    // All files enabled when store is determined at runtime.
                    var enabled = target == AppStore.NotSpecified;
                    // Otherwise this file must be needed on the target.
                    enabled |= mapping.Value == target;

                    var path = $"{UdpBinPath}/{mapping.Key}";
                    var importer = (PluginImporter)AssetImporter.GetAtPath(path);

                    if (importer != null)
                    {
                        importer.SetCompatibleWithPlatform(BuildTarget.Android, enabled);
                    }
                    else
                    {
                        // Search for any occurrence of this file
                        // Only fail if more than one found
                        var paths = FindPaths(mapping.Key);

                        if (paths.Length == 1)
                        {
                            importer = (PluginImporter)AssetImporter.GetAtPath(paths[0]);
                            importer.SetCompatibleWithPlatform(BuildTarget.Android, enabled);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// To enable or disable importation of assets at build-time, collect Project-relative
        /// paths matching <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">Name of file to search for in this Project</param>
        /// <returns>Relative paths matching <paramref name="filename"/></returns>
        public static string[] FindPaths(string filename)
        {
            var paths = new List<string>();

            var guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(filename));

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var foundFilename = Path.GetFileName(path);

                if (filename == foundFilename)
                {
                    paths.Add(path);
                }
            }

            return paths.ToArray();
        }

        private static void SaveConfig(AppStore enabled)
        {
            var configToSave = new StoreConfiguration(enabled);
            File.WriteAllText(ModePath, StoreConfiguration.Serialize(configToSave));
            AssetDatabase.ImportAsset(ModePath);
            config = configToSave;
        }

        internal static AppStore ConfiguredAppStore()
        {
            if (config == null)
            {
                return defaultAppStore;
            }

            return config.androidStore;
        }

        // Run me to configure the project's set of Android stores before build
        [PostProcessScene(0)]
        internal static void OnPostProcessScene()
        {
            if (File.Exists(ModePath))
            {
                try
                {
                    config = StoreConfiguration.Deserialize(File.ReadAllText(ModePath));
                    ConfigureProject(config.androidStore);
                }
                catch (Exception e)
                {
#if ENABLE_EDITOR_GAME_SERVICES
                    Debug.LogError("Unity IAP unable to strip undesired Android stores from build, check file: " + ModePath);
#else
                    Debug.LogError("Unity IAP unable to strip undesired Android stores from build, use menu (e.g. "
                        + SwitchStoreMenuItem + ") and check file: " + ModePath);
#endif
                    Debug.LogError(e);
                }
            }
        }

        [MenuItem(IapMenuConsts.MenuItemRoot + "/Configure...", false, 0)]
        private static void ConfigurePurchasingSettings()
        {
#if ENABLE_EDITOR_GAME_SERVICES && SERVICES_SDK_CORE_ENABLED
            var path = PurchasingSettingsProvider.GetSettingsPath();
            SettingsService.OpenProjectSettings(path);
#elif UNITY_2020_3_OR_NEWER
            ServicesUtils.OpenServicesProjectSettings(PurchasingService.instance.projectSettingsPath, PurchasingService.instance.settingsProviderClassName);
#else
            EditorApplication.ExecuteMenuItem("Window/General/Services");
#endif
            GameServicesEventSenderHelpers.SendTopMenuConfigure();
        }
    }
}
