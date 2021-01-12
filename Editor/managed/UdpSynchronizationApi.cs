using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    public static class UdpSynchronizationApi
    {

        internal const string kOAuthClientId = "channel_editor";

        // Although a client secret is here, it doesn't matter
        // because the user information is also secured by user's token
        private const string kOAuthClientSecret = "B63AFB324DE3D12A13827340019D1EE3";

        private const string kHttpVerbGET = "GET";
        private const string kHttpVerbPOST = "POST";
        private const string kHttpVerbPUT = "PUT";

        private const string kContentType = "Content-Type";
        private const string kApplicationJson = "application/json";
        private const string kAuthHeader = "Authorization";

        private static string kUnityWebRequestTypeString = "UnityEngine.Networking.UnityWebRequest";
        private static string kUploadHandlerRawTypeString = "UnityEngine.Networking.UploadHandlerRaw";
        private static string kDownloadHandlerBufferTypeString = "UnityEngine.Networking.DownloadHandlerBuffer";
        private const string kUnityOAuthNamespace = "UnityEditor.Connect.UnityOAuth";

        private static void CheckUdpBuildConfig()
        {
            Type udpBuildConfig = BuildConfigInterface.GetClassType();
            if (udpBuildConfig == null)
            {
                Debug.LogError("Cannot Retrieve Build Config Endpoints for UDP. Please make sure the UDP package is installed");
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Get Access Token according to authCode.
        /// </summary>
        /// <param name="authCode"> Acquired by UnityOAuth</param>
        /// <returns></returns>
        public static object GetAccessToken(string authCode)
        {
            CheckUdpBuildConfig();

            TokenRequest req = new TokenRequest();
            req.code = authCode;
            req.client_id = kOAuthClientId;
            req.client_secret = kOAuthClientSecret;
            req.grant_type = "authorization_code";
            req.redirect_uri = BuildConfigInterface.GetIdEndpoint();
            return asyncRequest(kHttpVerbPOST, BuildConfigInterface.GetApiEndpoint(), "/v1/oauth2/token", null, req);
        }

        public static object GetOrgId(string accessToken, string projectGuid)
        {
            CheckUdpBuildConfig();

            string api = "/v1/core/api/projects/" + projectGuid;
            return asyncRequest(kHttpVerbGET, BuildConfigInterface.GetApiEndpoint(), api, accessToken, null);
        }

        public static object CreateStoreItem(string accessToken, string orgId, IapItem iapItem)
        {
            CheckUdpBuildConfig();

            string api = "/v1/store/items";
            iapItem.ownerId = orgId;
            return asyncRequest(kHttpVerbPOST, BuildConfigInterface.GetUdpEndpoint(), api, accessToken, iapItem);
        }

        public static object UpdateStoreItem(string accessToken, IapItem iapItem)
        {
            CheckUdpBuildConfig();

            string api = "/v1/store/items/" + iapItem.id;
            return asyncRequest(kHttpVerbPUT, BuildConfigInterface.GetUdpEndpoint(), api, accessToken, iapItem);
        }

        public static object SearchStoreItem(string accessToken, string orgId, string appItemSlug)
        {
            CheckUdpBuildConfig();

            string api = "/v1/store/items/search?ownerId=" + orgId +
                         "&ownerType=ORGANIZATION&start=0&count=20&type=IAP&masterItemSlug=" + appItemSlug;
            return asyncRequest(kHttpVerbGET, BuildConfigInterface.GetUdpEndpoint(), api, accessToken, null);
        }

        // Return UnityWebRequest instance
        private static object asyncRequest(string method, string url, string api, string token,
            object postObject)
        {
            Type unityWebRequestType = UnityWebRequestType();
            object request = Activator.CreateInstance(unityWebRequestType, url + api, method);

            if (postObject != null)
            {
                string postData = HandlePostData(JsonUtility.ToJson(postObject));
                byte[] postDataBytes = Encoding.UTF8.GetBytes(postData);

                // Set UploadHandler
                // Equivalent : request.uploadHandler = (UploadHandler) new UploadHandlerRaw(postDataBytes);
                Type uploadHanlderRawType = UDPReflectionUtils.GetTypeByName(kUploadHandlerRawTypeString);

                var uploadHandlerRaw = Activator.CreateInstance(uploadHanlderRawType, postDataBytes);
                PropertyInfo uploadHandlerInfo =
                    unityWebRequestType.GetProperty("uploadHandler", UDPReflectionUtils.k_InstanceBindingFlags);
                uploadHandlerInfo.SetValue(request, uploadHandlerRaw, null);
            }

            // Set up downloadHandler
            // Equivalent: request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
            var downloadHandlerInstance = Activator.CreateInstance(UDPReflectionUtils.GetTypeByName(kDownloadHandlerBufferTypeString));
            var downloadHandlerProperty =
                unityWebRequestType.GetProperty("downloadHandler", UDPReflectionUtils.k_InstanceBindingFlags);
            downloadHandlerProperty.SetValue(request, downloadHandlerInstance, null);


            // Set up header
            // Equivalent : request.SetRequestHeader("key", "value");
            MethodInfo setRequestHeaderMethodInfo =
                unityWebRequestType.GetMethod("SetRequestHeader", UDPReflectionUtils.k_InstanceBindingFlags);

            setRequestHeaderMethodInfo.Invoke(request, new object[] {kContentType, kApplicationJson});
            if (token != null)
            {
                setRequestHeaderMethodInfo.Invoke(request, new object[] {kAuthHeader, "Bearer " + token});
            }

            // Send Web Request
            // Equivalent: request.SendWebRequest()/request.Send()
            MethodInfo sendWebRequest = unityWebRequestType.GetMethod("SendWebRequest");
            if (sendWebRequest == null)
            {
                sendWebRequest = unityWebRequestType.GetMethod("Send");
            }

            sendWebRequest.Invoke(request, null);

            return request;
        }

        // Try to find UnityOAuth in assembly, if not found, udp will not be available.
        // Also, the version must larger or equal to 5.6.1
        internal static bool CheckUdpAvailability()
        {
            bool hasOAuth = GetUnityOAuthType() != null;
            return hasOAuth;
        }

        internal static bool CheckUdpCompatibility()
        {
            Type udpBuildConfig = BuildConfigInterface.GetClassType();
            if (udpBuildConfig == null)
            {
                Debug.LogError("Cannot Retrieve Build Config Endpoints for UDP. Please make sure the UDP package is installed");
                return false;
            }

			var udpVersion = BuildConfigInterface.GetVersion();
			int majorVersion = 0;
			int.TryParse(udpVersion.Split('.')[0], out majorVersion);

			return majorVersion >= 2;
		}

        // A very tricky way to deal with the json string, need to be improved
        // en-US and zh-CN will appear in the JSON and Unity JsonUtility cannot
        // recognize them to variables. So we change this to a string (remove "-").
        private static string HandlePostData(string oldData)
        {
            string newData = oldData.Replace("thisShouldBeENHyphenUS", "en-US");
            newData = newData.Replace("thisShouldBeZHHyphenCN", "zh-CN");
            Regex re = new Regex("\"\\w+?\":\"\",");
            newData = re.Replace(newData, "");
            re = new Regex(",\"\\w+?\":\"\"");
            newData = re.Replace(newData, "");
            re = new Regex("\"\\w+?\":\"\"");
            newData = re.Replace(newData, "");
            return newData;
        }

        #region Reflection Utils

        // Using UnityOAuth through reflection to avoid error on Unity lower than 5.6.1.
        internal static Type GetUnityOAuthType()
        {
            return UDPReflectionUtils.GetTypeByName(kUnityOAuthNamespace);
        }

        internal static Type UnityWebRequestType()
        {
            return UDPReflectionUtils.GetTypeByName(kUnityWebRequestTypeString);
        }

        // get UnityWebRequest.isDone property
        internal static bool IsUnityWebRequestDone(object request)
        {
            var isDoneProperty =
                UnityWebRequestType().GetProperty("isDone", UDPReflectionUtils.k_InstanceBindingFlags);

            return (bool) isDoneProperty.GetValue(request, null);
        }

        // Get UnityWebRequest.error property
        internal static string UnityWebRequestError(object request)
        {
            var errorProperty = UnityWebRequestType().GetProperty("error", UDPReflectionUtils.k_InstanceBindingFlags);

            return (string) errorProperty.GetValue(request, null);
        }

        // UnityWebRequest.responseCode
        internal static long UnityWebRequestResponseCode(object request)
        {
            var responseProperty = UnityWebRequestType()
                .GetProperty("responseCode", UDPReflectionUtils.k_InstanceBindingFlags);
            return (long) responseProperty.GetValue(request, null);
        }

        // UnityWebRequest.DownloadHandler.text
        internal static string UnityWebRequestResultString(object request)
        {
            var downloadHandlerProperty =
                UnityWebRequestType().GetProperty("downloadHandler", UDPReflectionUtils.k_InstanceBindingFlags);

            object downloadHandler = downloadHandlerProperty.GetValue(request, null);

            var textProperty = UDPReflectionUtils.GetTypeByName(kDownloadHandlerBufferTypeString)
                .GetProperty("text", UDPReflectionUtils.k_InstanceBindingFlags);

            return (string) textProperty.GetValue(downloadHandler, null);
        }

        #endregion
    }

    #region model

    [Serializable]
    public class TokenRequest
    {
        public string code;
        public string client_id;
        public string client_secret;
        public string grant_type;
        public string redirect_uri;
        public string refresh_token;
    }

    [Serializable]
    public class PriceSets
    {
        public PurchaseFee PurchaseFee = new PurchaseFee();
    }

    [Serializable]
    public class PurchaseFee
    {
        public string priceType = "CUSTOM";
        public PriceMap priceMap = new PriceMap();
    }

    [Serializable]
    public class PriceMap
    {
        public List<PriceDetail> DEFAULT = new List<PriceDetail>();
    }

    [Serializable]
    public class PriceDetail
    {
        public string price;
        public string currency = "USD";
    }

    [Serializable]
    public class GeneralResponse
    {
        public string message;
    }

    [Serializable]
    public class Properties
    {
        public string description;
    }

    [Serializable]
    public class IapItemResponse : GeneralResponse
    {
        public string id;
    }

    [Serializable]
    public class IapItem
    {
        public string id;
        public string slug;
        public string name;
        public string masterItemSlug;
        public bool consumable = true;
        public string type = "IAP";
        public string status = "STAGE";
        public string ownerId;
        public string ownerType = "ORGANIZATION";

        public PriceSets priceSets = new PriceSets();

        public Properties properties = new Properties();

        public string ValidationCheck()
        {
            if (string.IsNullOrEmpty(slug))
            {
                return "Please fill in the ID";
            }

            if (string.IsNullOrEmpty(name))
            {
                return "Please fill in the title";
            }

            if (properties == null || string.IsNullOrEmpty(properties.description))
            {
                return "Please fill in the description";
            }

            return "";
        }
    }

    [Serializable]
    public class TokenInfo : GeneralResponse
    {
        public string access_token;
        public string refresh_token;
    }

    [Serializable]
    public class IapItemSearchResponse : GeneralResponse
    {
        public int total;
        public List<IapItem> results;
    }

    struct ReqStruct
    {
        public object request; // UnityWebRequest object
        public GeneralResponse resp;
        public ProductCatalogEditor.ProductCatalogItemEditor itemEditor;
        public IapItem iapItem;
    }

    [Serializable]
    public class OrgIdResponse : GeneralResponse
    {
        public string org_foreign_key;
    }

    [Serializable]
    public class OrgRoleResponse : GeneralResponse
    {
        public List<string> roles;
    }

    [Serializable]
    public class ErrorResponse : GeneralResponse
    {
        public string code;
        public ErrorDetail[] details;
    }

    [Serializable]
    public class ErrorDetail
    {
        public string field;
        public string reason;
    }

    #endregion
}
