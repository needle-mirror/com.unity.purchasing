#nullable enable

using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    public interface IAsyncDelayer
    {
        Task Delay(int delayMilliseconds);
    }
}
