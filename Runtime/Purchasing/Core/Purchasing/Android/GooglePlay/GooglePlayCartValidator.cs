using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayCartValidator : StoreCartValidator
    {
        [Preserve]
        internal GooglePlayCartValidator()
            : base(GooglePlay.DisplayName, new SingleProductSingleQuantityCartValidator()) { }
    }
}
