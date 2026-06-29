using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnityEditor.Purchasing")]
[assembly: InternalsVisibleTo("UnityEditor.Purchasing.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Purchasing.Apple")]
[assembly: InternalsVisibleTo("Unity.Purchasing.AppleMacos")]
[assembly: InternalsVisibleTo("Unity.Purchasing.AppleMacosStub")]
[assembly: InternalsVisibleTo("Unity.Purchasing.AppleStub")]
[assembly: InternalsVisibleTo("Unity.Purchasing.Codeless")]
[assembly: InternalsVisibleTo("Unity.Purchasing.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.Purchasing.Security")]
[assembly: InternalsVisibleTo("Unity.Purchasing.SecurityStub")]
[assembly: InternalsVisibleTo("Unity.Purchasing.Stores")]
[assembly: InternalsVisibleTo("Unity.Purchasing.Utilities")]
[assembly: InternalsVisibleTo("Unity.Purchasing.PaymentOption")]
#if UNITY_INCLUDE_TESTS
//Needed for Moq to generate mocks from internal interfaces
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("UnityEditor.Authoring.Tests")]
#endif
