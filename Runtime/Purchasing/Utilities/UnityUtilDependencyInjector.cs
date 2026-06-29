using Purchasing.Utilities;
using Uniject;
using UnityEngine;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing.Utilities
{
    static class UnityUtilDependencyInjector
    {
        const string k_IAPMonoBehaviourUtil = "IAPMonoBehaviourUtil";

        internal static UnityEngine.Purchasing.Extension.UnityUtil CreateUnityUtils()
        {
            IDependencyInjectionService di = new DependencyInjectionService();
            di.AddInstance(CreateMonoBehaviourUtil());
            di.AddService<UnityThreadUtils>();
            di.AddService<UnityEngine.Purchasing.Extension.UnityUtil>();
            return di.GetInstance<UnityEngine.Purchasing.Extension.UnityUtil>();
        }

        static MonoBehaviourUtil CreateMonoBehaviourUtil()
        {
            var gameObject = new GameObject(k_IAPMonoBehaviourUtil);
            Object.DontDestroyOnLoad(gameObject);
            gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            return gameObject.AddComponent<MonoBehaviourUtil>();
        }
    }
}
