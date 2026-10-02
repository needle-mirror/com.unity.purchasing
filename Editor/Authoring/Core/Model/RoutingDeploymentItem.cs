using Unity.Services.DeploymentApi.Editor;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    class RoutingDeploymentItem : DeploymentItemBase
    {
        public RoutingDeploymentItem(string path) : base(path) { }

        public override string Type => "Routing Config";

        public ProviderRoutingConfig RoutingConfig { get; set; }

        public bool Validate()
        {
            ClearTypedStates(ProviderRoutingConfig.ValidationStateType);

            if (RoutingConfig == null)
            {
                return false;
            }

            var states = RoutingConfig.Validate();
            var hasError = false;
            foreach (var state in states)
            {
                States.Add(state);
                if (state.Level == SeverityLevel.Error)
                {
                    hasError = true;
                }
            }

            return !hasError;
        }

        public override string ToString()
        {
            return $"'{Path}'";
        }
    }
}
