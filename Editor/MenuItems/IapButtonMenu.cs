using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// IAPButtonMenu class creates options in menus to create the <see cref="CodelessIAPButton"/>.
    /// </summary>
    public static class IAPButtonMenu
    {
        /// <summary>
        /// Add option to create a CodelessIAPButton from the GameObject menu.
        /// </summary>
        [MenuItem("GameObject/" + IapMenuConsts.PurchasingDisplayName + "/IAP Button", false, 10)]
        public static void GameObjectCreateUnityCodelessIAPButton()
        {
            CreateUnityCodelessIAPButtonInternal("IAP Button");

            GenericEditorMenuItemClickEventSenderHelpers.SendGameObjectMenuAddCodelessIapButtonEvent();
        }

        /// <summary>
        /// Add option to create a CodelessIAPButton from the Window/UnityIAP menu.
        /// </summary>
        [MenuItem(IapMenuConsts.MenuItemRoot + "/Create IAP Button", false, 100)]
        public static void CreateUnityCodelessIAPButton()
        {
            CreateUnityCodelessIAPButtonInternal("IAP Button");

            GenericEditorMenuItemClickEventSenderHelpers.SendIapMenuAddCodelessIapButtonEvent();
            GameServicesEventSenderHelpers.SendTopMenuCreateCodelessIapButtonEvent();
        }

        static void CreateUnityCodelessIAPButtonInternal(string name)
        {
            var emptyObject = ItemCreationUtility.CreateGameObject(name);

            if (emptyObject)
            {
                emptyObject.AddComponent<CodelessIAPButton>();
            }
        }
    }
}
