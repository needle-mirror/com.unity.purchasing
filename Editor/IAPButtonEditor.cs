using UnityEngine;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// Customer Editor class for the IAPButton. This class handle how the IAPButton should represent itself in the UnityEditor.
    /// </summary>
//disable Warning CS0618  IAPButton is deprecated, please use CodelessIAPButton instead.
#pragma warning disable 0618
    [CustomEditor(typeof(IAPButton))]
    [CanEditMultipleObjects]
    public class IAPButtonEditor : AbstractIAPButtonEditor
    {
        /// <summary>
        /// Event trigger when <c>IAPButton</c> is enabled in the scene.
        /// </summary>
        public void OnEnable()
        {
            OnEnableInternal();
        }

        /// <summary>
        /// Event trigger when trying to draw the <c>IAPButton</c> in the inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            OnInspectorGuiInternal();
        }
    }
}
