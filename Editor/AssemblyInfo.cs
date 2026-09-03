using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnityEditor.Purchasing.EditorTests")]
[assembly: InternalsVisibleTo("Unity.IntegrationTests")]
[assembly: InternalsVisibleTo("Unity.RuntimeTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

// Declares that com.unity.purchasing requires Insights data collection: keeps the
// Insights module in player builds and enables collection for this package.
// UNITY_INSIGHTS_REQUIREMENTS_API is a versionDefine (Unity >= 6000.7.0a7) so the
// declaration only compiles on Editors that provide the attribute.
// ENABLE_CLOUD_SERVICES_ENGINE_DIAGNOSTICS gates the Insights module itself — the
// attribute only exists when the module is compiled.
#if UNITY_INSIGHTS_REQUIREMENTS_API && ENABLE_CLOUD_SERVICES_ENGINE_DIAGNOSTICS
[assembly: UnityEditor.InsightsEditor.RequiresInsights]
#endif

