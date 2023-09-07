using UnityEngine.Networking;

namespace UnityEditor.Purchasing
{
    static class UnityWebRequestExtensions
    {
        public static bool IsResultTransferSuccess(this UnityWebRequest request)
        {
            return request.isDone && request.result == UnityWebRequest.Result.Success;
        }

        public static bool IsResultProtocolError(this UnityWebRequest request)
        {
            return request.isDone && request.result == UnityWebRequest.Result.ProtocolError;
        }

        public static bool IsResponseCodeOk(this UnityWebRequest request)
        {
            return (request.responseCode / 100) == 2;
        }
    }
}
