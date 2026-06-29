using UnityEngine.Purchasing.Stores;

namespace UnityEngine.Purchasing.WebshopService
{
    static class WebshopServiceProvider
    {
        public static IWebshopClientWrapper Instance()
        {
            return s_Instance ??= new WebshopClientWrapper();
        }

        static IWebshopClientWrapper s_Instance;
    }
}
