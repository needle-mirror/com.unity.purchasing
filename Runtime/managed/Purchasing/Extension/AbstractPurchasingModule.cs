namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// Base class for Purchasing Modules.
    ///
    /// In addition to providing helper methods, use of an abstract
    /// class allows addition of IPurchasingModule methods without
    /// breaking compatibility with existing plugins.
    /// </summary>
    public abstract class AbstractPurchasingModule : IPurchasingModule
    {
        protected IPurchasingBinder m_Binder;

        public void Configure(IPurchasingBinder binder)
        {
            this.m_Binder = binder;
            Configure();
        }

        protected void RegisterStore(string name, IStore a)
        {
            m_Binder.RegisterStore(name, a);
        }

        protected void BindExtension<T>(T instance) where T : IStoreExtension
        {
            m_Binder.RegisterExtension(instance);
        }

        protected void BindConfiguration<T>(T instance) where T : IStoreConfiguration
        {
            m_Binder.RegisterConfiguration(instance);
        }

        public abstract void Configure();
    }
}
