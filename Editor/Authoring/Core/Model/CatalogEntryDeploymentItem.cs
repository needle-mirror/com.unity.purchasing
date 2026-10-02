using System.Linq;
using System.Runtime.Serialization;
using Unity.Services.DeploymentApi.Editor;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    [DataContract]
    class CatalogEntryDeploymentItem : DeploymentItemBase
    {
        public CatalogEntryDeploymentItem(string path) : base(path) { }

        public override string Type => "Catalog Item";

        public CatalogItem CatalogItem { get; set; }

        public bool Validate(CatalogItem previousItem)
        {
            ClearTypedStates(CatalogItem.ValidationStateType);

            var states = CatalogItem.Validate();
            foreach (var state in states)
            {
                States.Add(state);
            }

            if (previousItem != null && CatalogItem.ProductType != previousItem.ProductType)
            {
                States.Add(new AssetState(
                    "Product Type change",
                    "The product type has changed. This can lead to unintended consequences in the future of the catalog.",
                    SeverityLevel.Warning,
                    CatalogItem.ValidationStateType));
            }

            return !states.Any(s => s.Level == SeverityLevel.Error);
        }

        public override string ToString()
        {
            if (Path == "Remote")
            {
                return CatalogItem.uSku;
            }
            return $"'{Path}'";
        }
    }
}
