using System.Runtime.Serialization;

namespace UnityEditor.Purchasing.Editor.Authoring.Core
{
    public enum StoreId
    {
        [EnumMember(Value = "apple")]
        Apple,
        [EnumMember(Value = "google")]
        Google,
        [EnumMember(Value = "xbox")]
        XboxStore,
        [EnumMember(Value = "applemacos")]
        MacAppStore,
    }
}
