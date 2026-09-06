using UnityEditor;
using UnityEngine;

/// <summary>ClassTypeName 的 Inspector 绘制器：在标签后追加基类名提示。</summary>
[CustomPropertyDrawer(typeof(ClassTypeName))]
public class ClassTypeNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = attribute as ClassTypeName;
        if (attr != null && attr.type != null)
        {
            label.text = $"{label.text}（基类: {attr.type.Name}）";
        }

        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
