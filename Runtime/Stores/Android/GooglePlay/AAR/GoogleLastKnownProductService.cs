using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    class GoogleLastKnownProductService: IGoogleLastKnownProductService
    {
        string m_LastKnownProductId = null;
        public string GetLastKnownProductId()
        {
            return m_LastKnownProductId;
        }

        public void SetLastKnownProductId(string lastKnownProductId)
        {
            m_LastKnownProductId = lastKnownProductId;
        }
    }
}
