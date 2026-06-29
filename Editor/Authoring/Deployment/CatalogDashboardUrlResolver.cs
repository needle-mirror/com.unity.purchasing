using System;
using System.Threading.Tasks;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.Core.Editor.OrganizationHandler;
using Unity.Services.DeploymentApi.Editor;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    class CatalogDashboardUrlResolver : ICatalogDashboardUrlResolver
    {
        readonly IEnvironmentsApi m_EnvironmentsApi;
        readonly IProjectIdentifierProvider m_ProjectIdProvider;
        readonly IOrganizationHandler m_OrganizationHandler;

        public CatalogDashboardUrlResolver(
            IEnvironmentsApi environmentsApi,
            IProjectIdentifierProvider projectIdProvider,
            IOrganizationHandler organizationHandler)
        {
            m_EnvironmentsApi = environmentsApi;
            m_ProjectIdProvider = projectIdProvider;
            m_OrganizationHandler = organizationHandler;
        }

        public Task<string> PurchasingDashboardUrlGetter(string assetName)
        {
            var projectId = m_ProjectIdProvider.ProjectId;
            var envId = m_EnvironmentsApi.ActiveEnvironmentId;
            var orgId = m_OrganizationHandler.Key;
            var url = $"https://cloud.unity.com/home/organizations/{orgId}/projects/{projectId}/environments/{envId}/in-app-purchase/catalog";
            if (!string.IsNullOrEmpty(assetName))
            {
                url += $"?q={Uri.EscapeDataString(assetName)}";
            }
            return Task.FromResult(url);
        }
    }
}
