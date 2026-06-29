// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.11.

using UnityEditor;

namespace Unity.Purchasing.Editor.Shared.UI
{
    class DisplayDialog : IDisplayDialog
    {
        public bool Show(string title, string content, string ok, string cancel)
        {
            return EditorUtility.DisplayDialog(title, content, ok, cancel);
        }
    }
}
