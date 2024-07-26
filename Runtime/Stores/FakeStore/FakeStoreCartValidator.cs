#nullable enable

namespace UnityEngine.Purchasing
{
    class FakeStoreCartValidator : StoreCartValidator
    {
        public FakeStoreCartValidator() : base(FakeAppStore.DisplayName, new NonNullCartValidator()) { }
    }
}
