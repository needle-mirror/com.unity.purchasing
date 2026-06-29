// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Purchasing.Shared.EditorUtils
{
    class ApplyRevertChangeTracker<T> where T : ScriptableObject, ICopyable<T>
    {
        public SerializedObject SerializedObject { get; }

        readonly SerializedObject m_EditorTarget;

        public ApplyRevertChangeTracker(SerializedObject editorTarget)
        {
            m_EditorTarget = editorTarget;
            SerializedObject = DeepCopy(editorTarget);
        }

        public bool IsDirty()
        {
            var property = SerializedObject.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.hasMultipleDifferentValues)
                {
                    continue;
                }

                for (var i = 0; i < SerializedObject.targetObjects.Length; i++)
                {
                    var stateObj = SerializedObject.targetObjects[i];
                    var editorObj = m_EditorTarget.targetObjects[i];

                    // strings for SerializedProperty are char arrays
                    // FieldInfo does not treat strings as arrays
                    // need to skip array checks if value is a string
                    if (property.isArray
                        && property.propertyType != SerializedPropertyType.String)
                    {
                        var listState = (IList)FieldValue(property.propertyPath, (T)stateObj);
                        var listEditor = (IList)FieldValue(property.propertyPath, (T)editorObj);

                        if (listState.Count != listEditor.Count)
                        {
                            return true;
                        }

                        for (var j = 0; j < listState.Count; ++j)
                        {
                            if (!Equals(listState[j], listEditor[j]))
                            {
                                return true;
                            }
                        }
                    }
                    else if (!Equals(FieldValue(property.propertyPath, (T)stateObj), FieldValue(property.propertyPath, (T)editorObj)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Apply()
        {
            CopyValues(SerializedObject, m_EditorTarget);
        }

        public void Reset()
        {
            CopyValues(m_EditorTarget, SerializedObject);
        }

        static void CopyValues(SerializedObject from, SerializedObject to)
        {
            for (var i = 0; i < from.targetObjects.Length; i++)
            {
                ((ICopyable<T>)from.targetObjects[i]).CopyTo((T)to.targetObjects[i]);
            }

            to.UpdateIfRequiredOrScript();
        }

        static object FieldValue(string path, T target)
        {
            return typeof(T)
                .GetField(path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(target);
        }

        static SerializedObject DeepCopy(SerializedObject source)
        {
            return new SerializedObject(source.targetObjects.Select(o => DeepCopy((T)o)).ToArray());
        }

        static Object DeepCopy(T source)
        {
            var inst = ScriptableObject.CreateInstance<T>();
            source.CopyTo(inst);
            return inst;
        }
    }
}
