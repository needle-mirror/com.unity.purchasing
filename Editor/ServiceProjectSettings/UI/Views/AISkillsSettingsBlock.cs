using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    internal class AISkillsSettingsBlock : IPurchasingSettingsUIBlock
    {
        const string k_OpenSkillsFolderBtn = "OpenSkillsFolderButton";
        const string k_InstallSkillsBtn = "InstallSkillsButton";
        const string k_PackageSkillsRelativePath = "Packages/com.unity.purchasing/Editor/AI/skills/packages/unity-iap";
        const string k_ClaudeSkillsDestFolder = "skills/in-app-purchases";

        VisualElement m_Block;

        public VisualElement GetUIBlockElement()
        {
            m_Block = SettingsUIUtils.CloneUIFromTemplate(UIResourceUtils.aiSkillsUxmlPath);

            if (m_Block == null)
            {
                return new VisualElement();
            }

            SetupStyleSheets();
            SetupButtons();

            return m_Block;
        }

        void SetupStyleSheets()
        {
            m_Block.AddStyleSheetPath(UIResourceUtils.purchasingCommonUssPath);
            m_Block.AddStyleSheetPath(EditorGUIUtility.isProSkin
                ? UIResourceUtils.purchasingDarkUssPath
                : UIResourceUtils.purchasingLightUssPath);
        }

        void SetupButtons()
        {
            m_Block.Q<Button>(k_OpenSkillsFolderBtn).clicked += OpenSkillsFolder;

            var installButton = m_Block.Q<Button>(k_InstallSkillsBtn);
            if (ClaudeConfigDirectoryExists())
            {
                installButton.clicked += InstallSkillsToClaudeCode;
            }
            else
            {
                installButton.style.display = DisplayStyle.None;
            }
        }

        static string GetResolvedSkillsPath()
        {
            return Path.GetFullPath(k_PackageSkillsRelativePath);
        }

        static string GetClaudeConfigDirectory()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".claude");
        }

        static bool ClaudeConfigDirectoryExists()
        {
            return Directory.Exists(GetClaudeConfigDirectory());
        }

        static void OpenSkillsFolder()
        {
            var skillsPath = GetResolvedSkillsPath();
            if (Directory.Exists(skillsPath))
            {
                EditorUtility.RevealInFinder(skillsPath);
            }
            else
            {
                Debug.LogWarning($"[IAP] AI Skills directory not found at: {skillsPath}");
            }
        }

        static void InstallSkillsToClaudeCode()
        {
            var sourcePath = GetResolvedSkillsPath();
            if (!Directory.Exists(sourcePath))
            {
                EditorUtility.DisplayDialog("Install Failed",
                    $"Source skills directory not found at:\n{sourcePath}", "OK");
                return;
            }

            var destPath = Path.Combine(GetClaudeConfigDirectory(), k_ClaudeSkillsDestFolder);

            if (Directory.Exists(destPath) && Directory.GetFileSystemEntries(destPath).Length > 0)
            {
                if (!EditorUtility.DisplayDialog("Overwrite Existing Skills?",
                    $"Skills already exist at:\n{destPath}\n\nDo you want to overwrite them?",
                    "Overwrite", "Cancel"))
                {
                    return;
                }
            }

            try
            {
                CopyDirectoryRecursive(sourcePath, destPath);
                EditorUtility.DisplayDialog("Skills Installed",
                    $"AI skills have been installed to:\n{destPath}", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Install Failed",
                    $"Failed to copy skills:\n{ex.Message}", "OK");
                Debug.LogException(ex);
            }
        }

        static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                if (string.Equals(Path.GetExtension(file), ".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(subDir);
                CopyDirectoryRecursive(subDir, Path.Combine(destDir, dirName));
            }
        }
    }
}
