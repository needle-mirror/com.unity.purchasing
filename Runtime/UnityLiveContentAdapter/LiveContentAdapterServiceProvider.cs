using UnityEngine.Purchasing.Stores;

namespace UnityEngine.Purchasing.LiveContentAdapterService
{
    static class LiveContentAdapterServiceProvider
    {
        public static ILiveContentAdapterClientWrapper Instance()
        {
            return s_Instance ??= new LiveContentAdapterClientWrapper();
        }

        static ILiveContentAdapterClientWrapper s_Instance;
    }
}
