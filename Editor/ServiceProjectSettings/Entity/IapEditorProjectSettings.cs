using UnityEngine;

namespace UnityEditor.Purchasing
{
    [FilePath("ProjectSettings/Packages/com.unity.purchasing/Settings.asset", FilePathAttribute.Location.ProjectFolder)]
    class IapEditorProjectSettings : ScriptableSingleton<IapEditorProjectSettings>
    {
        [SerializeField]
        bool m_IgnoreGoogleLicenseKeyWarning;

        internal static bool IgnoreGoogleLicenseKeyWarning
        {
            get => instance.m_IgnoreGoogleLicenseKeyWarning;
            set
            {
                instance.m_IgnoreGoogleLicenseKeyWarning = value;
                instance.Save(true);
            }
        }
    }
}
