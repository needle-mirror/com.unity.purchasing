using UnityEngine.Networking;

namespace UnityEditor.Purchasing
{
    public interface IWebRequest
    {
        UnityWebRequest BuildWebRequest(string uri);
    }
}
