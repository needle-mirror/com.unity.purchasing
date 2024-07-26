#nullable enable

using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    class AsyncDelayer : IAsyncDelayer
    {
        public Task Delay(int delayMilliseconds)
        {
            return Task.Delay(delayMilliseconds);
        }
    }
}
