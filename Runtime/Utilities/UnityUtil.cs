using System;
using System.Collections;
using System.Collections.Generic;
using Purchasing.Utilities;
using Uniject;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Extension
{
    class UnityUtil : IUtil
    {
        IThreadUtils m_ThreadUtils;
        IMonoBehaviourUtil m_MonoBehaviourUtils;

        [Preserve]
        public UnityUtil(IThreadUtils threadUtils, IMonoBehaviourUtil monoBehaviorUtil)
        {
            m_ThreadUtils = threadUtils;
            m_MonoBehaviourUtils = monoBehaviorUtil;
        }

        static readonly List<RuntimePlatform> s_PcControlledPlatforms = new List<RuntimePlatform>
        {
            RuntimePlatform.LinuxPlayer,
            RuntimePlatform.OSXEditor,
            RuntimePlatform.OSXPlayer,
            RuntimePlatform.WindowsEditor,
            RuntimePlatform.WindowsPlayer,
        };

        public T[] GetAnyComponentsOfType<T>() where T : class
        {
            var objects = m_MonoBehaviourUtils.GetGameObjects();
            var result = new List<T>();
            foreach (var o in objects)
            {
                foreach (var mono in o.GetComponents<MonoBehaviour>())
                {
                    if (mono is T)
                    {
                        result.Add(mono as T);
                    }
                }
            }

            return result.ToArray();
        }

        public DateTime currentTime => DateTime.Now;

        public string persistentDataPath => Application.persistentDataPath;

        /// <summary>
        /// WARNING: Reading from this may require special application privileges.
        /// </summary>
        public string deviceUniqueIdentifier => SystemInfo.deviceUniqueIdentifier;

        public string unityVersion => Application.unityVersion;

        public string cloudProjectId => Application.cloudProjectId;

        public string userId => PlayerPrefs.GetString("unity.cloud_userid", String.Empty);

        public string gameVersion => Application.version;

        public UInt64 sessionId => UInt64.Parse(PlayerPrefs.GetString("unity.player_sessionid", "0"));

        public RuntimePlatform platform => Application.platform;

        public bool isEditor => Application.isEditor;

        public string deviceModel => SystemInfo.deviceModel;

        public string deviceName => SystemInfo.deviceName;

        public DeviceType deviceType => SystemInfo.deviceType;

        public string operatingSystem => SystemInfo.operatingSystem;

        public int screenWidth => Screen.width;

        public int screenHeight => Screen.height;

        public float screenDpi => Screen.dpi;

        public string screenOrientation => Screen.orientation.ToString();

        object IUtil.InitiateCoroutine(IEnumerator start)
        {
            return m_MonoBehaviourUtils.StartCoroutine(start);
        }

        void IUtil.InitiateCoroutine(IEnumerator start, int delay)
        {
            m_MonoBehaviourUtils.DelayedCoroutine(start, delay);
        }

        public void RunOnMainThread(Action runnable)
        {
            if (m_ThreadUtils.IsRunningOnMainThread)
            {
                runnable();
            }
            else
            {
                m_ThreadUtils.PostAsync(runnable);
            }
        }

        public object GetWaitForSeconds(int seconds)
        {
            return new WaitForSeconds(seconds);
        }

        public static bool PcPlatform()
        {
            return s_PcControlledPlatforms.Contains(Application.platform);
        }

        readonly List<Action<bool>> pauseListeners = new List<Action<bool>>();

        public void AddPauseListener(Action<bool> runnable)
        {
            pauseListeners.Add(runnable);
        }

        public bool IsClassOrSubclass(Type potentialBase, Type potentialDescendant)
        {
            return potentialDescendant.IsSubclassOf(potentialBase) || potentialDescendant == potentialBase;
        }

        internal const string ObsoleteUpgradeToIAPV5Message =
            "This API is deprecated. Please upgrade to the new APIs introduced in IAP v5. For more information, visit the IAP manual: https://docs.unity.com/ugs/en-us/manual/iap/manual/upgrade-to-iap-v5";
    }
}
