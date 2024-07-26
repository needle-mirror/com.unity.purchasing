#nullable enable

namespace UnityEngine.Purchasing
{
    interface IDependencyInjectionService
    {
        void AddInstance(object instance);
        void AddService<T>() where T : class;
        T GetInstance<T>() where T : class;
    }
}
