// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System.Collections.Generic;

namespace Unity.Purchasing.Editor.Shared.Clients
{
    class AdminApiHeaders<T>
    {
        readonly string m_GatewayToken;

        public AdminApiHeaders(string gatewayToken)
        {
            m_GatewayToken = gatewayToken;
        }

        public IDictionary<string, string> ToDictionary()
        {
            var packageInfo = ReadPackageInfo();

            return new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {m_GatewayToken}" },
                { "x-client-id", $"{packageInfo.name}@{packageInfo.version}"}
            };
        }

        static UnityEditor.PackageManager.PackageInfo ReadPackageInfo()
        {
            return UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(T).Assembly);
        }
    }
}
