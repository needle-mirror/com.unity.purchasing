using System.Runtime.CompilerServices;

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("UnityEditor.Purchasing.Tests.Editor.Authoring")]
[assembly: InternalsVisibleTo("Unity.Purchasing.Tests.Runtime.Authoring")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#endif

[assembly: InternalsVisibleTo("UnityEditor.Purchasing.Authoring")]
[assembly: InternalsVisibleTo("Unity.Services.Cli.Purchasing")]
[assembly: InternalsVisibleTo("Unity.Services.Cli.Purchasing.UnitTest")]
