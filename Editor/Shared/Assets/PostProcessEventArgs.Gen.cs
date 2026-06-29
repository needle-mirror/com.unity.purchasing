// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;

namespace Unity.Purchasing.Editor.Shared.Assets
{
    record PostProcessEventArgs
    {
        public string[] ImportedAssetPaths = Array.Empty<string>();
        public string[] DeletedAssetPaths = Array.Empty<string>();
        public string[] MovedAssetPaths = Array.Empty<string>();
        public string[] MovedFromAssetPaths = Array.Empty<string>();
        public bool DidDomainReload;
    }
}
