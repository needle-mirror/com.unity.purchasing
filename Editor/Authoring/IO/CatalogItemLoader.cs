using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Model;
using UnityEngine;

namespace UnityEditor.Purchasing.Editor.Authoring.IO
{
    class CatalogItemLoader : ICatalogLoader
    {
        enum ErrorMessage
        {
            ParsingError,
            Error
        }

        readonly IFileSystem m_FileSystem;
        readonly JsonSerializerSettings m_SerializerSettings;

        public CatalogItemLoader(IFileSystem fileSystem)
        {
            m_FileSystem = fileSystem;

            m_SerializerSettings = EditorCatalogItem.GetSerializationSettings();
        }

        public async Task<CatalogEntryDeploymentItem> ReadCatalog(string path, CancellationToken token)
        {
            var fileName = Path.GetFileName(path);
            var deploymentItem = new CatalogEntryDeploymentItem(path);
            try
            {
                var text = await m_FileSystem.ReadAllText(path, token);
                var model = FromFile(JsonConvert.DeserializeObject<EditorCatalogItem>(text, m_SerializerSettings) !);
                deploymentItem.CatalogItem = model;
                var stem = Path.GetFileNameWithoutExtension(fileName);
                model.CatalogListingId = CatalogItem.CatalogListingIdPrefix + stem;
                if (model.uSku == null)
                    model.uSku = stem;
            }
            catch (IOException e)
            {
                deploymentItem.Status = Statuses.GetFailedToLoad(e, deploymentItem.Path);;
            }
            catch (JsonException e)
            {
                deploymentItem.Status = Statuses.GetFailedToRead(e, deploymentItem.Path);
            }

            return deploymentItem;
        }

        public async Task CreateOrUpdateCatalog(CatalogEntryDeploymentItem deployableEntryDeploymentItem, CancellationToken token)
        {
            var fileName = Path.GetFileNameWithoutExtension(deployableEntryDeploymentItem.Path);
            var id = deployableEntryDeploymentItem.CatalogItem.uSku;
            try
            {
                // By default, use filename as the Id, unless overriden.
                // This reduces cognitive complexity in having multiple "ids" for the same file
                var fileModel = ToFile(deployableEntryDeploymentItem.CatalogItem);
                if (fileModel.uSku == fileName)
                    fileModel.uSku = null;
                var text = JsonConvert.SerializeObject(deployableEntryDeploymentItem.CatalogItem, m_SerializerSettings);
                await m_FileSystem.WriteAllText(deployableEntryDeploymentItem.Path, text, token);
            }
            catch (JsonException e)
            {
                deployableEntryDeploymentItem.Status = Statuses.GetFailedToSerialize(e, deployableEntryDeploymentItem.Path);
            }
            catch (Exception e)
            {
                deployableEntryDeploymentItem.Status = Statuses.GetFailedToWrite(e, deployableEntryDeploymentItem.Path);
            }
        }

        public async Task DeleteCatalog(CatalogEntryDeploymentItem entryDeploymentItem, CancellationToken token)
        {
            try
            {
                await m_FileSystem.Delete(entryDeploymentItem.Path, token);
            }
            catch (IOException e)
            {
                entryDeploymentItem.Status = Statuses.GetFailedToDelete(e, entryDeploymentItem.Path);
            }
        }

        public void DeserializeAndPopulateFromPath(CatalogEntryDeploymentItem config, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path), "cannot deserialize a config with an empty path.");
            }

            try
            {
                var content = File.ReadAllText(path);
                var editorCatalogItem = JsonConvert.DeserializeObject<EditorCatalogItem>(content, m_SerializerSettings);
                var stem = Path.GetFileNameWithoutExtension(path);
                editorCatalogItem.CatalogListingId = CatalogItem.CatalogListingIdPrefix + stem;
                if (string.IsNullOrEmpty(editorCatalogItem.uSku))
                {
                    editorCatalogItem.uSku = stem;
                }

                config.CatalogItem = editorCatalogItem;
            }
            catch (Exception e)
                when(e is SerializationException
                    or JsonSerializationException
                    or JsonReaderException)
            {
                throw new CatalogDeserializationException(
                    ErrorMessage.ParsingError.ToString(),
                    e.Message,
                    e);
            }
            catch (Exception e)
            {
                throw new CatalogDeserializationException(
                    ErrorMessage.Error.ToString(),
                    e.Message,
                    e);
            }
        }

        static CatalogItem FromFile(EditorCatalogItem fileModel)
        {
            return new CatalogItem()
            {
                uSku = fileModel.uSku,
                ProductType = fileModel.ProductType,
                PricingDetails = fileModel.PricingDetails
            };
        }

        static EditorCatalogItem ToFile(CatalogItem fileModel)
        {
            return new EditorCatalogItem()
            {
                uSku = fileModel.uSku,
                ProductType = fileModel.ProductType,
                PricingDetails = fileModel.PricingDetails,
            };
        }
    }

    class CatalogDeserializationException : Exception
    {
        public string ErrorMessage;
        public string Details;

        public CatalogDeserializationException(string message, string details, Exception exception)
            : base(message, exception)
        {
            ErrorMessage = message;
            Details = details;
        }
    }
}
