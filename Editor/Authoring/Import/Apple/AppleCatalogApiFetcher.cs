using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Purchasing.Editor.Authoring.Import;
using UnityEditor.Purchasing.Editor.Authoring.PurchasingAdminApi;
using Unity.Purchasing.Editor.Shared.Clients;
using Unity.Services.Core.Editor;
using Unity.Services.Core.Editor.Environments;

namespace UnityEditor.Purchasing.Editor.Authoring.Import.Apple
{
internal class AppleCatalogApiFetcher : ICatalogFetcher
{
    const string k_Store = "apple";

    readonly IPlatformCatalogApi m_Api;
    readonly IAccessTokens m_TokenProvider;

    public string SecretKey { get; set; } = "purchasing_ios";

    public PlatformCatalogImportRequest.SecretScopeEnum SecretScope { get; set; } =
        PlatformCatalogImportRequest.SecretScopeEnum.Project;

    public AppleCatalogApiFetcher(IPlatformCatalogApi api, IAccessTokens tokenProvider)
    {
        m_Api = api ?? throw new ArgumentNullException(nameof(api));
        m_TokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    public async Task<List<ImportedCatalogEntry>> FetchCatalogEntries()
    {
        var accessToken = await m_TokenProvider.GetServicesGatewayTokenAsync();
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException(
                "Unity Services token is empty. Ensure you are signed in to the Unity Editor.");

        var bundleId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);
        if (string.IsNullOrWhiteSpace(bundleId))
            throw new InvalidOperationException(
                "iOS Bundle Identifier is missing. Configure it in Project Settings > Player.");

        var projectId = CloudProjectSettings.projectId;
        if (string.IsNullOrWhiteSpace(projectId))
            throw new InvalidOperationException(
                "Unity project is not linked. Open Project Settings > Services to link a project.");

        var environmentId = EnvironmentsApi.Instance?.ActiveEnvironmentId;
        if (environmentId == null || environmentId == Guid.Empty)
            throw new InvalidOperationException(
                "No active environment configured. Open Project Settings > Services > Environments.");

        var headers = new AdminApiHeaders<AppleCatalogApiFetcher>(accessToken);
        m_Api.Configuration.DefaultHeaders = headers.ToDictionary();

        var request = new PlatformCatalogImportRequest(
            secretKey: SecretKey,
            secretScope: SecretScope,
            appIdentifier: bundleId);

        var response = await m_Api.ImportPlatformCatalog(projectId, environmentId.ToString(), k_Store, request);

        if (!response.IsSuccessful)
        {
            if (response.StatusCode == 401)
                throw new UnauthorizedAccessException(
                    "Platform catalog import failed (401): access token is invalid or expired.");

            throw new InvalidOperationException(
                $"Platform catalog import failed ({response.StatusCode}): {response.ErrorText}");
        }

        return MapProducts(response.Data);
    }

    static List<ImportedCatalogEntry> MapProducts(PlatformCatalogResponse catalog)
    {
        var entries = new List<ImportedCatalogEntry>();
        if (catalog?.Products == null)
            return entries;

        foreach (var product in catalog.Products)
        {
            var entry = new ImportedCatalogEntry
            {
                Sku = product.USKU ?? string.Empty,
                ProductType = product.Type ?? string.Empty,
                ImageUrl = product.ImageUrl ?? string.Empty
            };

            if (product.ProductDetails != null && product.ProductDetails.Count > 0)
            {
                var detail = product.ProductDetails[0];
                entry.Title = detail.Title ?? string.Empty;
                entry.Description = detail.Description ?? string.Empty;
                entry.Language = detail.Language ?? string.Empty;
            }

            if (product.Pricing != null && product.Pricing.Count > 0)
            {
                var pricing = product.Pricing[0];
                entry.CurrencyCode = pricing.CurrencyCode ?? string.Empty;
                entry.Price = pricing.Amount / 1_000_000.0;
            }

            entries.Add(entry);
        }

        return entries;
    }
}
}
