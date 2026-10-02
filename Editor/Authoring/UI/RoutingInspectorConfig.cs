using System;
using System.Collections.Generic;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEngine;

namespace UnityEditor.Purchasing.Editor.Authoring.UI
{
    [Serializable]
    class RoutingInspectorConfig : ScriptableObject
    {
        [Serializable]
        public struct RoutingMapping
        {
            [Tooltip("User tag from Live Releases (e.g. 'default', 'beta-tester', 'asia').")]
            public string Tag;
            [Tooltip("Ordered list of payment provider IDs for this tag (e.g. 'stripe', 'codapay', 'mock').")]
            public List<string> Providers;
        }

        [Tooltip("Tag-to-provider routing rules. Each tag maps to an ordered list of payment providers.")]
        public List<RoutingMapping> Mappings;

        public void Initialize(ProviderRoutingConfig config)
        {
            Mappings = new List<RoutingMapping>();
            if (config?.Mappings != null)
            {
                foreach (var mapping in config.Mappings)
                {
                    Mappings.Add(new RoutingMapping
                    {
                        Tag = mapping.Key,
                        Providers = mapping.Value != null
                            ? new List<string>(mapping.Value)
                            : new List<string>()
                    });
                }
            }
        }

        public ProviderRoutingConfig ToProviderRoutingConfig()
        {
            var routingConfig = new ProviderRoutingConfig
            {
                Mappings = new Dictionary<string, List<string>>()
            };

            if (Mappings != null)
            {
                foreach (var mapping in Mappings)
                {
                    var tag = mapping.Tag ?? string.Empty;
                    routingConfig.Mappings[tag] =
                        mapping.Providers != null
                            ? new List<string>(mapping.Providers)
                            : new List<string>();
                }
            }

            return routingConfig;
        }

        public List<AssetState> Validate()
        {
            var states = new List<AssetState>();

            if (Mappings != null)
            {
                var seenTags = new HashSet<string>();
                foreach (var mapping in Mappings)
                {
                    var tag = mapping.Tag ?? string.Empty;
                    if (!seenTags.Add(tag))
                    {
                        var displayTag = string.IsNullOrEmpty(tag) ? "(empty)" : tag;
                        states.Add(new AssetState(
                            $"Duplicate tag '{displayTag}'",
                            $"Tag '{displayTag}' appears more than once. Each tag must be unique.",
                            SeverityLevel.Error,
                            ProviderRoutingConfig.ValidationStateType));
                    }
                }
            }

            var config = ToProviderRoutingConfig();
            states.AddRange(config.Validate());
            return states;
        }
    }
}
