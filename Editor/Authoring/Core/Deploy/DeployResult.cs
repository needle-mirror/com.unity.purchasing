using System.Collections.Generic;
using Unity.Services.DeploymentApi.Editor;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Deploy
{
    class DeployResult
    {
        public IReadOnlyList<IDeploymentItem> Deployed { get; set; }
    }
}
