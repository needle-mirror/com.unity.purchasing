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
            return FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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
