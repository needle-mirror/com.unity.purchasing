// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System.IO;
using System.Linq;

namespace Unity.Purchasing.Editor.Shared.Infrastructure.IO
{
    static class PathUtils
    {
        const char k_NameSeparator = '.';
        internal const string k_Assets = "Assets";

        public static string Join(params string[] args)
        {
            var joinedPath = Path.Combine(args).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return joinedPath;
        }

        public static string GetRelativePath(string relativeTo, string path)
        {
            string reference = Path.GetFullPath(relativeTo);
            string fullPath = Path.GetFullPath(path);

            var refSplit = reference.Split(Path.DirectorySeparatorChar);
            var pathSplit = fullPath.Split(Path.DirectorySeparatorChar);

            int count = 0;

            while (count < refSplit.Length
                   && count < pathSplit.Length
                   && refSplit[count] == pathSplit[count])
            {
                count++;
            }

            var keep = pathSplit.Skip(count);
            var beginning = Enumerable.Repeat($"..", refSplit.Length - count);
            var final = beginning.Concat(keep);
            return Path.Combine(final.ToArray());
        }

        /// <summary>
        /// Returns the initial filename excluding everything after the first period.
        /// Example: /Something/foo.bar.baz => foo
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetFileNameWithoutSubExtension(string path)
        {
            var file = Path.GetFileName(path);
            var separator = file.IndexOf(k_NameSeparator);
            if (separator != -1)
            {
                file = file.Substring(0, separator);
            }
            return file;
        }
    }
}
