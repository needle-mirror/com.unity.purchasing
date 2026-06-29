// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;
using System.Collections.Generic;

namespace Unity.Purchasing.Editor.Shared.DependencyInversion
{
    sealed class Scope : List<IDisposable>, IDisposable
    {
        public bool Strict { get; }

        public Scope(bool strict = false)
        {
            Strict = strict;
        }

        public void Dispose()
        {
            foreach (var disposable in this)
            {
                disposable.Dispose();
            }
        }
    }
}
