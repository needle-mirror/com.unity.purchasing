using System.Collections;
using UnityEngine;

namespace Purchasing.Utilities
{
    [HideInInspector]
    [AddComponentMenu("")]
    class MonoBehaviourUtil : MonoBehaviour, IMonoBehaviourUtil
    {
        public GameObject[] GetGameObjects()
        {
#if UNITY_6000_4_OR_NEWER
            return FindObjectsByType<GameObject>();
#else
// Obsolete: FindObjectsSortMode
#pragma warning disable 618, 612
            return FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#pragma warning restore 618, 612
#endif
        }

        public IEnumerator DelayedCoroutine(IEnumerator coroutine, int delay)
        {
            yield return new WaitForSeconds(delay);
            StartCoroutine(coroutine);
        }

        Coroutine IMonoBehaviourUtil.StartCoroutine(IEnumerator start)
        {
            return StartCoroutine(start);
        }
    }
}
