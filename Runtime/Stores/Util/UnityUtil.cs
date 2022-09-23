using System;
using System.Collections;
using System.Collections.Generic;
using Uniject;

namespace UnityEngine.Purchasing.Extension
{
    [HideInInspector]
    [AddComponentMenu("")]
    internal class UnityUtil : MonoBehaviour, IUtil
    {
        private static readonly List<Action> s_Callbacks = new List<Action>();
        private static volatile bool s_CallbacksPending;

        private static readonly List<RuntimePlatform> s_PcControlledPlatforms = new List<RuntimePlatform>
        {
            RuntimePlatform.LinuxPlayer,
            RuntimePlatform.OSXEditor,
            RuntimePlatform.OSXPlayer,
            RuntimePlatform.WindowsEditor,
            RuntimePlatform.WindowsPlayer,
        };

        public T[] GetAnyComponentsOfType<T>() where T : class
        {
            var objects = (GameObject[])FindObjectsOfType(typeof(GameObject));
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
            return StartCoroutine(start);
        }

        void IUtil.InitiateCoroutine(IEnumerator start, int delay)
        {
            DelayedCoroutine(start, delay);
        }

        public void RunOnMainThread(Action runnable)
        {
            lock (s_Callbacks)
            {
                s_Callbacks.Add(runnable);
                s_CallbacksPending = true;
            }
        }

        public object GetWaitForSeconds(int seconds)
        {
            return new WaitForSeconds(seconds);
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        public static T FindInstanceOfType<T>() where T : MonoBehaviour
        {
            return (T)FindObjectOfType(typeof(T));
        }

        public static T LoadResourceInstanceOfType<T>() where T : MonoBehaviour
        {
            return ((GameObject)Instantiate(Resources.Load(typeof(T).ToString()))).GetComponent<T>();
        }

        public static bool PcPlatform()
        {
            return s_PcControlledPlatforms.Contains(Application.platform);
        }

        private IEnumerator DelayedCoroutine(IEnumerator coroutine, int delay)
        {
            yield return new WaitForSeconds(delay);
            StartCoroutine(coroutine);
        }

        private void Update()
        {
            if (!s_CallbacksPending)
            {
                return;
            }
            // We copy our actions to another array to avoid
            // locking the queue whilst we process them.
            Action[] copy;
            lock (s_Callbacks)
            {
                if (s_Callbacks.Count == 0)
                {
                    return;
                }

                copy = new Action[s_Callbacks.Count];
                s_Callbacks.CopyTo(copy);
                s_Callbacks.Clear();
                s_CallbacksPending = false;
            }

            foreach (var action in copy)
            {
                action();
            }
        }

        private readonly List<Action<bool>> pauseListeners = new List<Action<bool>>();
        public void AddPauseListener(Action<bool> runnable)
        {
            pauseListeners.Add(runnable);
        }

        public void OnApplicationPause(bool paused)
        {
            foreach (var listener in pauseListeners)
            {
                listener(paused);
            }
        }

        public bool IsClassOrSubclass(Type potentialBase, Type potentialDescendant)
        {
            return potentialDescendant.IsSubclassOf(potentialBase) || potentialDescendant == potentialBase;
        }
    }
}
