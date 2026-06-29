using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Editor.Authoring.Import.UI
{
    /// <summary>
    /// Draws provider-specific configuration UI.
    /// Separated from auth/fetch logic to honour the Single Responsibility Principle.
    /// </summary>
    interface IConfigDrawer
    {
        /// <summary>
        /// Creates and returns a UIToolkit visual element for the configuration UI.
        /// This allows the drawer to be used in UIToolkit contexts.
        /// </summary>
        VisualElement CreateConfigUI();
    }
}
