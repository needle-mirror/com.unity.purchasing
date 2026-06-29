// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;

namespace Unity.Purchasing.Editor.Shared.DependencyInversion
{
    interface IScopedServiceProvider : IServiceProvider, IDisposable
    {
        IScopedServiceProvider CreateScope();
    }
}
