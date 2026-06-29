// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System.IO;

namespace Unity.Purchasing.Editor.Shared.Infrastructure.SystemEnvironment
{
    static class SystemEnvironmentPathUtils
    {
#if UNITY_EDITOR_WIN
        const string k_PathSeparator = ";";
#else
        const string k_PathSeparator = ":";
#endif
        const string k_Path = "PATH";

        public static bool DoesEnvironmentPathContain(string filePath)
        {
            var path = global::System.Environment.GetEnvironmentVariable(k_Path);
            if (string.IsNullOrEmpty(path))
                return false;

            var fileDirectory = Path.GetDirectoryName(filePath);
            return path.Contains(fileDirectory ?? string.Empty);
        }

        public static void AddProcessToPath(string processPath)
        {
            var processDirectory = Path.GetDirectoryName(processPath);
            global::System.Environment.SetEnvironmentVariable(
                k_Path,
                global::System.Environment.GetEnvironmentVariable(k_Path) + $"{k_PathSeparator}{processDirectory}");
        }
    }
}
