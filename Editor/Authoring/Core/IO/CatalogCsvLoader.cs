using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.IO
{
    class CatalogCsvLoader : ICatalogCsvLoader
    {
        readonly IFileSystem m_FileSystem;
        readonly ICatalogCsvParser m_CsvParser;

        public CatalogCsvLoader(IFileSystem fileSystem, ICatalogCsvParser csvParser)
        {
            m_FileSystem = fileSystem;
            m_CsvParser = csvParser;
        }

        public async Task<(List<CatalogItem> items, List<AssetState> issues)> ReadCatalog(
            string path,
            CancellationToken token)
        {
            var content = await m_FileSystem.ReadAllText(path, token);
            var items = m_CsvParser.Parse(content, out var issues);
            return (items, issues);
        }

        public async Task WriteCatalog(
            string path,
            List<CatalogItem> items,
            CancellationToken token)
        {
            var content = m_CsvParser.Serialize(items);
            await m_FileSystem.WriteAllText(path, content, token);
        }

        public async Task DeleteCatalog(string path, CancellationToken token)
        {
            await m_FileSystem.Delete(path, token);
        }
    }
}
