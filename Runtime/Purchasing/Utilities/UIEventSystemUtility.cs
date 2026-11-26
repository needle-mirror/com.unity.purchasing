using UnityEngine.EventSystems;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Creates the fallback <see cref="EventSystem"/> that the package's uGUI dialogs need when the
    /// scene has none, with an input module that works under the project's Active Input Handling.
    /// </summary>
    static class UIEventSystemUtility
    {
        internal static GameObject CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            // StandaloneInputModule reads UnityEngine.Input, which fails when Active Input Handling is
            // Input System only, so the dialog buttons would never fire.
#if IAP_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
            return eventSystem;
        }
    }
}
