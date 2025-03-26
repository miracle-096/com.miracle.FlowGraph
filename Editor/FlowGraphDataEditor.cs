using UnityEditor;
using UnityEngine;

namespace FlowGraph.Node
{
    // 添加双击打开功能
    [InitializeOnLoad]
    public class FlowGraphDataEditor
    {
        static FlowGraphDataEditor()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<FlowGraphData>(path);
                    if (asset != null)
                    {
                        FlowChartEditorWindow.OpenWindow(asset);
                        Event.current.Use();
                    }
                }
            }
        }
        
        [UnityEditor.MenuItem("Assets/Create/FlowGraph/FlowGraphData", false, priority = 0)]
        public static FlowGraphData CreateFlowGraphData()
        {
            var graphData = ScriptableObject.CreateInstance<FlowGraphData>();

            // 获取保存路径
            string path = "Assets";
            if (Selection.activeObject != null)
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(path))
                {
                    path = System.IO.Path.GetDirectoryName(path);
                }
            }

            // 创建唯一的文件名
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/NewFlowGraph.asset");

            // 保存资源
            AssetDatabase.CreateAsset(graphData, assetPath);
            AssetDatabase.SaveAssets();

            // 选中新创建的资源
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = graphData;
            return graphData;
        }
    }
}