using System;
using UnityEditor.Build;
using UnityEditor.Purchasing;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal class PurchasingImporter
    {
        static PurchasingImporter()
        {
            PurchasingSettings.ApplyEnableSettings(EditorUserBuildSettings.activeBuildTarget);
        }
    }
}
