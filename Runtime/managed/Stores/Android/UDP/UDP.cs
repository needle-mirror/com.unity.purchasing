using System;

namespace UnityEngine.Purchasing
{
    public class UDP
    {
        // Unity Distribution Platform (UDP) may target to various store,
        // e.g. Xiaomi, MooStore, etc. So the <code>Name</code> should
        // be more specific in this case.
        public static string Name
        {
            get
            {
                try
                {
                    return StoreServiceInterface.GetName();
                }
                catch (Exception)
                {
                    return "UDP";
                }
            }
        }
    }
}
