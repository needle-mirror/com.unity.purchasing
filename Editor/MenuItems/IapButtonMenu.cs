using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// IAPButtonMenu class creates options in menus to create the <see cref="IAPButton"/>.
    /// </summary>
    public static class IAPButtonMenu
    {
        /// <summary>
        /// Add option to create a IAPButton from the GameObject menu.
        /// </summary>
        [MenuItem("GameObject/" + IapMenuConsts.PurchasingDisplayName + "/IAP Button (Legacy)", false, 11)]
        public static void GameObjectCreateUnityIAPButton()
        {
            CreateUnityIAPButtonInternal("IAP Button (Legacy)");

            GenericEditorMenuItemClickEventSenderHelpers.SendGameObjectMenuAddIapButtonEvent();
        }

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
        /// Add option to create a IAPButton from the Window/UnityIAP menu.
        /// </summary>
        [MenuItem(IapMenuConsts.MenuItemRoot + "/Create IAP Button (Legacy)", false, 101)]
        public static void CreateUnityIAPButton()
        {
            CreateUnityIAPButtonInternal("IAP Button (Legacy)");

            GenericEditorMenuItemClickEventSenderHelpers.SendIapMenuAddIapButtonEvent();
            GameServicesEventSenderHelpers.SendTopMenuCreateIapButtonEvent();
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

        static void CreateUnityIAPButtonInternal(string name)
        {
            var buttonObject = ItemCreationUtility.CreateGameObject(name, typeof(Button));

            if (buttonObject)
            {
                //disable Warning CS0618  IAPButton is deprecated, please use CodelessIAPButton instead.
#pragma warning disable 0618
                var iapButton = buttonObject.AddComponent<IAPButton>();

                if (iapButton != null)
                {
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(iapButton);
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(iapButton);
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(iapButton);
                }
            }
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
