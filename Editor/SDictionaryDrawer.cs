using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameGC.Collections.Editor
{
    [CustomPropertyDrawer(typeof(SDictionary<,>))]
    public class SDictionaryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_keyValuePairs"), label,true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var target = property.FindPropertyRelative("_keyValuePairs");
            
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position,target , label, true);
            if (EditorGUI.EndChangeCheck())
            {
                var dict = GetProperty(property) as ITypeInfo;
                
                var type = dict.TKey;
                if (type.IsSubclassOf(typeof(Object)))
                {
                    var controlID = GUIUtility.GetControlID(FocusType.Passive) + 100;
                    EditorGUIUtility.ShowObjectPicker<Object>(null, true, $"t:{dict.TKey.Name}", controlID);
                }
            }
            
            string commandName = Event.current.commandName;
            if (commandName is "ObjectSelectorUpdated" or "ObjectSelectorClosed")
            {
                var dict = GetProperty(property) as IDictionary;
                dict.Add(EditorGUIUtility.GetObjectPickerObject(),null);
                
                var info = GetProperty(property) as ITypeInfo;
                object[] keys = GetProperty(target) as object[];
                var onstruc = typeof(SKeyValuePair<,>).GetConstructor(new[] {info.TKey, info.TValue}).Invoke(null, null);
                ArrayUtility.Add(ref keys,(object)onstruc);
                
                Debug.Log(commandName);
                target.InsertArrayElementAtIndex(target.arraySize-1);
                target.GetArrayElementAtIndex(target.arraySize - 1).FindPropertyRelative("Key")
                    .objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
            }
        }

        private static object GetProperty(SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("["))
                        .Replace("[", "")
                        .Replace("]", ""));
                    obj = GetValue(obj, elementName, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }
            }

            return obj;
        }
        private static object GetValue(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();
            var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f == null)
            {
                var p = type.GetProperty(name,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p == null) return null;
                return p.GetValue(source, null);
            }

            return f.GetValue(source);
        }

        private static object GetValue(object source, string name, int index)
        {
            var enumerable = GetValue(source, name) as IEnumerable;
            var enm = enumerable.GetEnumerator();
            while (index-- >= 0) enm.MoveNext();
            return enm.Current;
        }
    }
}