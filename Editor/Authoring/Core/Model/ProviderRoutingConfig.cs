using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Services.DeploymentApi.Editor;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    class ProviderRoutingConfig
    {
        internal const string ValidationStateType = "RoutingValidation";

        // Provider IDs are case-sensitive on the backend — always lower-case.
        internal static readonly string[] KnownProviders = { "stripe", "codapay", "mock" };

        [JsonProperty("mappings")]
        internal Dictionary<string, List<string>> Mappings { get; set; } = new();

        [JsonProperty("enforceProviderTagValidation")]
        internal bool EnforceProviderTagValidation { get; set; }

        internal static ProviderRoutingConfig CreateDefault()
        {
            return new ProviderRoutingConfig
            {
                Mappings = new Dictionary<string, List<string>>
                {
                    { "default", new List<string> { "stripe" } }
                }
            };
        }

        internal List<AssetState> Validate()
        {
            var states = new List<AssetState>();

            if (Mappings == null || Mappings.Count == 0)
            {
                states.Add(new AssetState(
                    "Empty mappings",
                    "No tag to provider mappings defined.",
                    SeverityLevel.Warning,
                    ValidationStateType));
                return states;
            }

            foreach (var mapping in Mappings)
            {
                ValidateMapping(mapping, states);
            }

            return states;
        }

        static void ValidateMapping(KeyValuePair<string, List<string>> mapping, List<AssetState> states)
        {
            ValidateMappingKey(mapping.Key, states);

            if (mapping.Value == null || mapping.Value.Count == 0)
            {
                states.Add(new AssetState(
                    $"No providers for '{mapping.Key}'",
                    "Each tag must map to at least one provider.",
                    SeverityLevel.Error,
                    ValidationStateType));
                return;
            }

            ValidateProviders(mapping.Key, mapping.Value, states);
        }

        static void ValidateMappingKey(string key, List<AssetState> states)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            states.Add(new AssetState(
                "Empty tag",
                "A mapping key (tag) is empty.",
                SeverityLevel.Error,
                ValidationStateType));
        }

        static void ValidateProviders(string tag, List<string> providers, List<AssetState> states)
        {
            foreach (var provider in providers)
            {
                if (Array.IndexOf(KnownProviders, provider) >= 0)
                {
                    continue;
                }

                states.Add(new AssetState(
                    $"Unknown provider '{provider}'",
                    $"Tag '{tag}' references unknown provider '{provider}'. "
                        + $"Known providers: {string.Join(", ", KnownProviders)}.",
                    SeverityLevel.Warning,
                    ValidationStateType));
            }
        }
    }
}
