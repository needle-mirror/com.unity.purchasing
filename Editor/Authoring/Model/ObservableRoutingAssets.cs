using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.Purchasing.Editor.Shared.Assets;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEngine;
using ILogger = UnityEditor.Purchasing.Editor.Authoring.Core.Logger.ILogger;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
    sealed class ObservableRoutingAssets : ObservableCollection<RoutingAsset>, IDisposable
    {
        const string k_DeserializationError = "DeserializationException";
        readonly ILogger m_Logger;
        readonly ObservableAssets<RoutingAsset> m_RoutingAssets;

        public ObservableCollection<IDeploymentItem> DeploymentItems { get; } =
            new ObservableCollection<IDeploymentItem>();

        public ObservableRoutingAssets(ILogger logger)
        {
            m_Logger = logger;
            m_RoutingAssets = new ObservableAssets<RoutingAsset>(
                new[] { Constants.RoutingFileExtension });

            foreach (var asset in m_RoutingAssets)
            {
                OnNewAsset(asset);
                DeploymentItems.Add(asset.RoutingDeploymentItem);
            }

            m_RoutingAssets.CollectionChanged += RoutingAssetsOnCollectionChanged;
        }

        public void Dispose()
        {
            m_RoutingAssets.CollectionChanged -= RoutingAssetsOnCollectionChanged;
        }

        void OnNewAsset(RoutingAsset asset)
        {
            PopulateModel(asset);
            Add(asset);
        }

        void PopulateModel(RoutingAsset asset, string assetPath = null)
        {
            assetPath ??= asset.Path;
            var deploymentItem = asset.RoutingDeploymentItem;
            deploymentItem.ClearTypedStates(k_DeserializationError);

            try
            {
                var json = File.ReadAllText(assetPath);
                var config = JsonConvert.DeserializeObject<ProviderRoutingConfig>(json)
                    ?? new ProviderRoutingConfig();
                deploymentItem.RoutingConfig = config;
                deploymentItem.Validate();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                deploymentItem.RoutingConfig = null;
                deploymentItem.States.Add(new AssetState(
                    "Failed to deserialize routing config",
                    exception.Message,
                    SeverityLevel.Error,
                    k_DeserializationError));
            }
        }

        void RoutingAssetsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
        {
            if (eventArgs.OldItems != null)
            {
                foreach (var oldItem in eventArgs.OldItems.Cast<RoutingAsset>())
                {
                    DeploymentItems.Remove(oldItem.RoutingDeploymentItem);
                    Remove(oldItem);
                }
            }

            if (eventArgs.NewItems != null)
            {
                foreach (var newItem in eventArgs.NewItems.Cast<RoutingAsset>())
                {
                    DeploymentItems.Add(newItem.RoutingDeploymentItem);
                    OnNewAsset(newItem);
                }
            }
        }

        public RoutingAsset GetOrCreateInstance(string assetPath)
        {
            foreach (var existing in m_RoutingAssets)
            {
                if (existing == null)
                {
                    continue;
                }

                if (assetPath == existing.Path)
                {
                    PopulateModel(existing, assetPath);
                    return existing;
                }
            }

            var asset = ScriptableObject.CreateInstance<RoutingAsset>();
            asset.Path = assetPath;
            PopulateModel(asset);
            return asset;
        }
    }
}
