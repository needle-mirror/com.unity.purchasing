using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    class ObfuscationGenerator
    {
        const string m_GeneratedCredentialsTemplateFilename = "IAPGeneratedCredentials.cs.template";
        const string m_GeneratedCredentialsTemplateFilenameNoExtension = "IAPGeneratedCredentials.cs";

        internal static string ObfuscateGoogleSecrets(string googlePlayPublicKey)
        {
            var googleError = WriteObfuscatedGooglePlayClassAsAsset(googlePlayPublicKey);

            AssetDatabase.Refresh();

            return googleError;
        }

        /// <summary>
        /// Generates specified obfuscated class files.
        /// </summary>
        internal static void ObfuscateSecrets(bool includeGoogle, ref string googleError, string googlePlayPublicKey)
        {
            try
            {
                if (includeGoogle)
                {
                    googleError = WriteObfuscatedGooglePlayClassAsAsset(googlePlayPublicKey);
                }
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogIAPWarning(e.StackTrace);
            }

            // Ensure all the Tangle classes exist, even if they were not generated at this time.
            if (!DoesGooglePlayTangleClassExist())
            {
                try
                {
                    WriteObfuscatedClassAsAsset(TangleFileConsts.k_GooglePlayClassPrefix, 0, new int[0], new byte[0], false);
                }
                catch (Exception e)
                {
                    Debug.unityLogger.LogIAPWarning(e.StackTrace);
                }
            }

            AssetDatabase.Refresh();
        }

        static string WriteObfuscatedGooglePlayClassAsAsset(string googlePlayPublicKey)
        {
            string googleError = null;
            var key = 0;
            var order = new int[0];
            var tangled = new byte[0];
            try
            {
                var bytes = Convert.FromBase64String(googlePlayPublicKey);
                order = new int[bytes.Length / 20 + 1];

                tangled = TangleObfuscator.Obfuscate(bytes, order, out key);
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogIAPWarning("Invalid Google Play Public Key. Generating incomplete " +
                    "credentials file. " + e);
                googleError =
                    "  The Google Play License Key is invalid. GooglePlayTangle was generated with incomplete credentials.";
            }
            WriteObfuscatedClassAsAsset(TangleFileConsts.k_GooglePlayClassPrefix, key, order, tangled, tangled.Length != 0);

            return googleError;
        }

        static string FullPathForTangleClass(string classnamePrefix)
        {
            return Path.Combine(TangleFileConsts.k_OutputPath, string.Format($"{classnamePrefix}{TangleFileConsts.k_ObfuscationClassSuffix}"));
        }

        internal static bool DoesGooglePlayTangleClassExist()
        {
            return ObfuscatedClassExists(TangleFileConsts.k_GooglePlayClassPrefix);
        }

        static bool ObfuscatedClassExists(string classnamePrefix)
        {
            return File.Exists(FullPathForTangleClass(classnamePrefix));
        }

        static void WriteObfuscatedClassAsAsset(string classnamePrefix, int key, int[] order, byte[] data, bool populated)
        {
            var substitutionDictionary = new Dictionary<string, string>()
            {
                {"{NAME}", classnamePrefix.ToString()},
                {"{KEY}", key.ToString()},
                {"{ORDER}", String.Format("{0}",String.Join(",", Array.ConvertAll(order, i => i.ToString())))},
                {"{DATA}", Convert.ToBase64String(data)},
                {"{POPULATED}", populated.ToString().ToLowerInvariant()} // Defaults to XML-friendly values
            };

            var templateText = LoadTemplateText(out var templateRelativePath);

            if (templateText != null)
            {
                var outfileText = templateText;

                // Apply the parameters to the template
                foreach (var pair in substitutionDictionary)
                {
                    outfileText = outfileText.Replace(pair.Key, pair.Value);
                }
                Directory.CreateDirectory(TangleFileConsts.k_OutputPath);
                File.WriteAllText(FullPathForTangleClass(classnamePrefix), outfileText);
            }
        }

        /// <summary>
        /// Loads the template file.
        /// </summary>
        /// <returns>The template file's text.</returns>
        /// <param name="templateRelativePath">Relative Assets/ path to template file.</param>
        static string LoadTemplateText(out string templateRelativePath)
        {
            var assetGUIDs =
                AssetDatabase.FindAssets(m_GeneratedCredentialsTemplateFilenameNoExtension);
            string templateGUID = null;
            templateRelativePath = null;

            if (assetGUIDs.Length > 0)
            {
                templateGUID = assetGUIDs[0];
            }
            else
            {
                Debug.unityLogger.LogIAPError($"Could not find template \"{m_GeneratedCredentialsTemplateFilename}\"");
            }

            string templateText = null;

            if (templateGUID != null)
            {
                templateRelativePath = AssetDatabase.GUIDToAssetPath(templateGUID);

                var templateAbsolutePath =
                    Path.GetDirectoryName(Application.dataPath)
                    + Path.DirectorySeparatorChar
                    + templateRelativePath;

                templateText = File.ReadAllText(templateAbsolutePath);
            }

            return templateText;
        }
    }
}
