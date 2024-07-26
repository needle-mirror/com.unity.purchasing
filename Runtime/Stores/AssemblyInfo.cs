using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: UnityEngine.Scripting.AlwaysLinkAssembly]
[assembly: InternalsVisibleTo("UnityEditor.Purchasing")]
[assembly: InternalsVisibleTo("UnityEditor.Purchasing.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Purchasing.RuntimeTests")]
//Needed for Moq to generate mocks from internal interfaces
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
