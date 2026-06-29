#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if IAP_UNITY_AUTH_RESTRICTED_TOKEN_ENABLED
using Unity.Services.Authentication;
#endif
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using UnityEngine.Purchasing.WebshopService.Apis.WebshopLink;
using UnityEngine.Purchasing.WebshopService.Http;
using UnityEngine.Purchasing.WebshopService.Models;
using UnityEngine.Purchasing.WebshopService.WebshopLink;

namespace UnityEngine.Purchasing.WebshopService
{
    internal class InternalWebshopService : IWebshopService
    {
        readonly IWebshopServiceExceptionMapper m_ServiceExceptionMapper;
        readonly IEnvironmentId m_EnvironmentId;
        readonly ICloudProjectId m_CloudProjectId;
        readonly IWebshopLinkApiClient m_WebshopLinkApiClient;
        readonly Configuration m_Configuration;

        internal InternalWebshopService(IAccessToken accessToken, IEnvironmentId environmentId, ICloudProjectId cloudProjectId, string? baseUrl = null)
        {
            m_EnvironmentId = environmentId;
            m_CloudProjectId = cloudProjectId;

            var url = baseUrl ?? "https://webshop.services.api.unity.com";
            m_WebshopLinkApiClient = new WebshopLinkApiClient(new HttpClient(), accessToken);
            m_Configuration = new Configuration(url, 10, 4, null);
            m_Configuration.Headers.Add("Unity-IAP-Package-Version", IAPVersion.Current);

            m_ServiceExceptionMapper = new WebshopServiceExceptionMapper();
        }

        public bool RestrictedTokensAvailable()
        {
#if IAP_UNITY_AUTH_RESTRICTED_TOKEN_ENABLED
            return true;
#else
            return false;
#endif
        }

        public async Task<WebshopLinkData> GetWebshopLink(
            string? catalogListingId,
            string? impressionId,
            string? locale,
            string? currencyCode,
            string? country,
            IReadOnlyList<WebshopExternalToken> externalTokens)
        {
            CheckForCloudProjectInfo();

            var linkParams = new List<WebshopLinkParam>();
            var sessionToken = await TryGenerateSessionToken();
            AddIfPresent(linkParams, WebshopLinkParam.TypeOptions.SessionToken, sessionToken);
            AddIfPresent(linkParams, WebshopLinkParam.TypeOptions.CatalogListingId, catalogListingId);
            AddIfPresent(linkParams, WebshopLinkParam.TypeOptions.ImpressionId, impressionId);
            AddIfPresent(linkParams, WebshopLinkParam.TypeOptions.Locale, locale);
            AddIfPresent(linkParams, WebshopLinkParam.TypeOptions.Currency, currencyCode);
            AddIfPresent(linkParams, WebshopLinkParam.TypeOptions.Country, country);

            foreach (var token in externalTokens)
            {
                linkParams.Add(new WebshopLinkParam(MapTokenType(token.Type), token.Token));
            }

            var request = new CreateWebshopLinkRequest(
                m_CloudProjectId.GetCloudProjectId(),
                m_EnvironmentId.EnvironmentId,
                new WebshopLinkRequest(linkParams)
            );

            return await m_ServiceExceptionMapper.InvokeAndMapServiceExceptions(async () =>
            {
                var response = await m_WebshopLinkApiClient.CreateWebshopLinkAsync(request, m_Configuration);
                return new WebshopLinkData(response.Result.Url, response.Result.Live);
            });
        }

        async Task<string?> TryGenerateSessionToken()
        {
#if IAP_UNITY_AUTH_RESTRICTED_TOKEN_ENABLED
            var tokenOptions = new RestrictedTokenOptions()
            {
                Services = new List<string> { "no-svc" }, SingleUse = true, TtlSeconds = 60
            };
            try
            {
                var tokenResult = await AuthenticationService.Instance.GenerateRestrictedTokenAsync(tokenOptions);
                return tokenResult.SessionToken;
            }
            catch (AuthenticationException e)
            {
                Debug.LogWarning("Could not generate session token: " + e.Message);
                return null;
            }
#else
            Debug.LogWarning("Players will be unable to log in to webshop. Please upgrade com.unity.services.authentication to 3.7.1 or above.");
            return await Task.FromResult<string?>(null);
#endif
        }

        static void AddIfPresent(List<WebshopLinkParam> list, WebshopLinkParam.TypeOptions type, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                list.Add(new WebshopLinkParam(type, value));
            }
        }

        static WebshopLinkParam.TypeOptions MapTokenType(WebshopExternalTokenType type) => type switch
        {
            WebshopExternalTokenType.AppleAcquisition => WebshopLinkParam.TypeOptions.AppleAcquisition,
            WebshopExternalTokenType.AppleServices => WebshopLinkParam.TypeOptions.AppleServices,
            WebshopExternalTokenType.AppleLinkOut => WebshopLinkParam.TypeOptions.AppleLinkOut,
            WebshopExternalTokenType.Google => WebshopLinkParam.TypeOptions.Google,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown webshop external token type."),
        };

        void CheckForCloudProjectInfo()
        {
            if (m_EnvironmentId?.EnvironmentId is null ||
                m_CloudProjectId?.GetCloudProjectId() is null
#if IAP_UNITY_AUTH_RESTRICTED_TOKEN_ENABLED
                || AuthenticationService.Instance is null
#endif
                )
            {
                throw new CloudProjectAuthenticationException();
            }
        }
    }
}
