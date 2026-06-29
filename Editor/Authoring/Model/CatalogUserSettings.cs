using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
    [FilePath("UserSettings/Packages/com.unity.purchasing/CatalogSettings.asset",
        FilePathAttribute.Location.ProjectFolder)]
    class CatalogUserSettings : ScriptableSingleton<CatalogUserSettings>
    {
        [SerializeField]
        List<string> m_PathsShownAsEntries = new();

        public static event Action SettingChanged;

        public static bool IsShownAsCsv(string path)
        {
            return !instance.m_PathsShownAsEntries.Contains(path);
        }

        internal static void TransferPath(string fromPath, string toPath)
        {
            var existingEntryIndex = instance.m_PathsShownAsEntries.IndexOf(fromPath);
            if (existingEntryIndex >= 0)
            {
                instance.m_PathsShownAsEntries[existingEntryIndex] = toPath;
                instance.Save(true);
            }
        }

        public static void SetShownAsCsv(string path, bool showAsCsv)
        {
            var contains = instance.m_PathsShownAsEntries.Contains(path);

            if (showAsCsv && !contains)
            {
                return;
            }

            if (!showAsCsv && contains)
            {
                return;
            }

            if (showAsCsv)
            {
                instance.m_PathsShownAsEntries.Remove(path);
            }
            else
            {
                instance.m_PathsShownAsEntries.Add(path);
            }

            instance.Save(true);
            SettingChanged?.Invoke();
        }
    }
}
