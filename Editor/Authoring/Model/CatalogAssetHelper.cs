using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
    static class CatalogAssetHelper
    {
        static readonly Regex k_InvalidChars = new(@"[^" + CatalogItem.ValidIdChars + "]", RegexOptions.Compiled);

        internal static string GenerateUniquePath(string folder, string baseName, string extension)
        {
            var path = folder + "/" + baseName + extension;
            if (!File.Exists(path))
            {
                return path;
            }
            for (var i = 1; i < 1000; i++)
            {
                var candidate = folder + "/" + baseName + "_" + i + extension;
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }
            return path;
        }

        static readonly string[] k_KnownExtensions = { Constants.CsvFileExtension, Constants.FileExtension };

        internal static string SanitizeAssetPath(string path)
        {
            var lastSlash = path.LastIndexOf('/');
            var directory = lastSlash >= 0 ? path.Substring(0, lastSlash) : "";
            var fileName = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
            var (name, extension) = SplitFileNameAndExtension(fileName);

            var sanitized = k_InvalidChars.Replace(name, "_");

            if (sanitized == name)
            {
                return path;
            }

            return GenerateUniquePath(directory, sanitized, extension);
        }

        static (string name, string extension) SplitFileNameAndExtension(string fileName)
        {
            foreach (var ext in k_KnownExtensions)
            {
                if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    return (fileName.Substring(0, fileName.Length - ext.Length), ext);
                }
            }

            var name = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            return (name, extension);
        }

        internal static string GetActiveFolderPath()
        {
            foreach (var obj in Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets))
            {
                var assetPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        return assetPath;
                    }
                    var slash = assetPath.LastIndexOf('/');
                    return slash >= 0 ? assetPath.Substring(0, slash) : assetPath;
                }
            }
            return "Assets";
        }
    }
}
