using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnityEditor.Purchasing")]
[assembly: InternalsVisibleTo("Unity.Purchasing")]
[assembly: InternalsVisibleTo("Unity.Purchasing.Stores")]
[assembly: InternalsVisibleTo("Unity.Purchasing.Codeless")]
[assembly: InternalsVisibleTo("Unity.Purchasing.RuntimeTests")]
//Needed for Moq to generate mocks from internal interfaces
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
