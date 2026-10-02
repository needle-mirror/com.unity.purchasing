using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.Core.Editor.OrganizationHandler;
using Unity.Services.DeploymentApi.Editor;
using UnityEngine;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    class RoutingOpenDashboardCommand : Command
    {
        readonly IEnvironmentsApi m_EnvironmentsApi;
        readonly IProjectIdentifierProvider m_ProjectIdProvider;
        readonly IOrganizationHandler m_OrganizationHandler;

        public override string Name => L10n.Tr("Open in Dashboard");

        public RoutingOpenDashboardCommand(
            IEnvironmentsApi environmentsApi,
            IProjectIdentifierProvider projectIdProvider,
            IOrganizationHandler organizationHandler)
        {
            m_EnvironmentsApi = environmentsApi;
            m_ProjectIdProvider = projectIdProvider;
            m_OrganizationHandler = organizationHandler;
        }

        public override Task ExecuteAsync(
            IEnumerable<IDeploymentItem> items,
            CancellationToken cancellationToken = default)
        {
            var orgId = m_OrganizationHandler.Key;
            var projectId = m_ProjectIdProvider.ProjectId;
            var envId = m_EnvironmentsApi.ActiveEnvironmentId;
            Application.OpenURL(
                $"https://cloud.unity.com/home/organizations/{orgId}/projects/{projectId}/environments/{envId}/in-app-purchase/payment-providers");
            return Task.CompletedTask;
        }
    }
}
