#nullable enable

using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    interface IAsyncDelayer
    {
        Task Delay(int delayMilliseconds);
    }
}
