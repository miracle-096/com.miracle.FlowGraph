using UnityEditor;
using UnityEngine;

namespace FlowGraph.Node
{
    [CustomEditor(typeof(FlowGraphData))]
    public class FlowGraphDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            FlowGraphData graphData = (FlowGraphData)target;

            // 绘制默认的 Inspector 界面
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
        
            // 添加打开编辑器窗口的按钮
            if (GUILayout.Button("打开 FlowGraph 编辑器"))
            {
                FlowChartEditorWindow.OpenWindow(graphData);
            }
        }
    }
}