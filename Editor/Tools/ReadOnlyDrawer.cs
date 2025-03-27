using UnityEngine;
using UnityEditor;

namespace FlowGraph.Node
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 保存之前的GUI启用状态
            bool previousEnabled = GUI.enabled;
            
            // 设置为禁用状态
            GUI.enabled = false;
            
            // 绘制属性
            EditorGUI.PropertyField(position, property, label, true);
            
            // 恢复之前的GUI启用状态
            GUI.enabled = previousEnabled;
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
} 