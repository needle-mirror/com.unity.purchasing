using System.Threading.Tasks;
using UnityEngine.Networking;

namespace UnityEditor.Purchasing
{
    // Whether Coda and Stripe are connected for an environment, as the dashboard sets them up.
    static class GetPaymentProviderConnectionsWebRequest
    {
        const string k_ShopConfigurationsKey = "shopConfigurations";
        const string k_CodapayKey = "codapay";
        const string k_StripeKey = "stripe";
        const string k_StripeOAuthConfiguredKey = "isOAuthConfigured";

        /// <returns>The connection state, or null when it couldn't be read (signed out, offline, no permission).</returns>
        internal static async Task<(bool coda, bool stripe)?> RequestAsync(string gatewayToken, string projectId, string environmentId)
        {
            var url = string.Format(PurchasingUrls.paymentProviderConfigurationUrl, projectId, environmentId);
            var (responseCode, body) = await GetAsync(url, gatewayToken);

            // An environment where no provider was ever set up has no configuration at all.
            if (responseCode == 404)
            {
                return (false, false);
            }

            if (responseCode / 100 != 2)
            {
                return null;
            }

            var shops = NetworkingUtils.GetJsonDictionaryWithinRawJsonDictionaryString(body, k_ShopConfigurationsKey);
            var stripe = NetworkingUtils.GetJsonDictionaryWithinJsonDictionary(shops, k_StripeKey);
            object isOAuthConfigured = null;
            stripe?.TryGetValue(k_StripeOAuthConfiguredKey, out isOAuthConfigured);

            return (NetworkingUtils.GetJsonDictionaryWithinJsonDictionary(shops, k_CodapayKey) != null,
                isOAuthConfigured is bool configured && configured);
        }

        static Task<(long responseCode, string body)> GetAsync(string url, string gatewayToken)
        {
            var request = UnityWebRequest.Get(url);
            request.suppressErrorsToConsole = true;
            request.SetRequestHeader("Authorization", $"Bearer {gatewayToken}");

            var result = new TaskCompletionSource<(long, string)>();
            request.SendWebRequest().completed += _ =>
            {
                using (request)
                {
                    result.TrySetResult((request.responseCode, request.downloadHandler.text));
                }
            };
            return result.Task;
        }
    }
}
