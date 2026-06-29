using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;

namespace UnityEditor.Purchasing.Editor.Authoring.IO
{
    class FileSystem : IFileSystem
    {
        public Task<string> ReadAllText(
            string path,
            CancellationToken token = default(CancellationToken))
        {
            return Task.FromResult(File.ReadAllText(path));
        }

        public Task WriteAllText(
            string path,
            string contents,
            CancellationToken token= default(CancellationToken))
        {
            File.WriteAllText(path, contents);
            return Task.CompletedTask;
        }

        public Task Delete(
            string path,
            CancellationToken token = default(CancellationToken))
        {
            File.Delete(path);
            return Task.CompletedTask;
        }
    }
}
