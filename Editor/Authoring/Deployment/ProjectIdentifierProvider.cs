using Unity.Services.DeploymentApi.Editor;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    class ProjectIdentifierProvider : IProjectIdentifierProvider
    {
        public string ProjectId => Deployments.Instance?.ProjectIdProvider?.ProjectId ?? string.Empty;
    }
}
