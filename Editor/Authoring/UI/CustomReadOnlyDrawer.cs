using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Authoring
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class CustomReadOnlyAttribute : PropertyAttribute { }

    [CustomPropertyDrawer(typeof(CustomReadOnlyAttribute))]
    internal sealed class CustomReadOnlyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var propertyField = new PropertyField(property, property.displayName);
            propertyField.bindingPath = property.propertyPath;
            propertyField.SetEnabled(false);
            return propertyField;
        }
    }
}
