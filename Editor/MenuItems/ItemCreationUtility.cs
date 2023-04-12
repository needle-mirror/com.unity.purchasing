using System;
using UnityEngine;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// This code is taken from the com.unity.2d.sprite@1.0.0 package
    /// </summary>
    static class ItemCreationUtility
    {
        internal static GameObject CreateGameObject(string name)
        {
            var parent = Selection.activeGameObject;
            var newGameObject = ObjectFactory.CreateGameObject(name);
            CreateGameObject(name, newGameObject, parent);
            return newGameObject;
        }

        internal static GameObject CreateGameObject(string name, params Type[] components)
        {
            var parent = Selection.activeGameObject;
            var newGameObject = ObjectFactory.CreateGameObject(name, components);
            CreateGameObject(name, newGameObject, parent);
            return newGameObject;
        }

        static void CreateGameObject(string name, GameObject newGameObject, GameObject parent)
        {
            newGameObject.name = name;
            Selection.activeObject = newGameObject;
            GOCreationCommands.Place(newGameObject, parent);
            if (EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D)
            {
                var position = newGameObject.transform.position;
                position.z = 0;
                newGameObject.transform.position = position;
            }

            Undo.RegisterCreatedObjectUndo(newGameObject, string.Format("Create {0}", name));
        }
    }
}
