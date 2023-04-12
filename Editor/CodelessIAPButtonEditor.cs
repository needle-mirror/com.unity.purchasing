using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// Customer Editor class for the CodelessIAPButton. This class handle how the CodelessIAPButton should represent itself in the UnityEditor.
    /// </summary>
    [CustomEditor(typeof(CodelessIAPButton))]
    [CanEditMultipleObjects]
    public class CodelessIAPButtonEditor : AbstractIAPButtonEditor
    {
        /// <summary>
        /// Event trigger when <c>CodelessIAPButton</c> is enabled in the scene.
        /// </summary>
        public void OnEnable()
        {
            OnEnableInternal();
        }

        /// <summary>
        /// Event trigger when trying to draw the <c>CodelessIAPButton</c> in the inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            OnInspectorGuiInternal();
        }
    }
}
