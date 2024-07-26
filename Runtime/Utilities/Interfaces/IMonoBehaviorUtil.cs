using System.Collections;
using UnityEngine;

namespace Purchasing.Utilities
{
    interface IMonoBehaviourUtil
    {
        GameObject[] GetGameObjects();

        IEnumerator DelayedCoroutine(IEnumerator coroutine, int delay);

        Coroutine StartCoroutine(IEnumerator start);
    }
}
