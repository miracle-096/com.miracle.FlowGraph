using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace FlowGraph.Node
{
    public class FlowChartEditorWindow : EditorWindow
    {
        public FlowGraphData currentGraphData;
        private const string LastGraphDataKey = "FlowChart_LastGraphData";

        public static void OpenWindow(FlowGraphData data)
        {
            var window = GetWindow<FlowChartEditorWindow>();
            if (window.currentGraphData == data && window.flowChartView != null)
            {
                window.flowChartView.ResetNodeView();
                return;
            }

            window = GetWindow<FlowChartEditorWindow>("FlowChart");
            window.currentGraphData = data;
            window.Init();
        }

        public FlowChartView flowChartView;
        private InspectorView inspectorView;

        private TextField gameObjectTextField;

        private void OnEnable()
        {
            // 注册编译完成事件
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            
            // 从EditorPrefs恢复上次打开的GraphData
            string lastGraphDataPath = EditorPrefs.GetString(LastGraphDataKey, "");
            if (!string.IsNullOrEmpty(lastGraphDataPath))
            {
                currentGraphData = AssetDatabase.LoadAssetAtPath<FlowGraphData>(lastGraphDataPath);
                if (currentGraphData != null)
                {
                    Init();
                }
            }
        }

        private void OnDisable()
        {
            // 取消注册编译完成事件
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            
            if (currentGraphData != null)
            {
                // 保存当前打开的GraphData路径
                string path = AssetDatabase.GetAssetPath(currentGraphData);
                EditorPrefs.SetString(LastGraphDataKey, path);
                
                // 保存当前图形数据，确保连接关系被持久化
                if (flowChartView != null)
                {
                    try
                    {
                        // 遍历节点，确保连接关系被正确保存
                        foreach (var node in flowChartView.nodes)
                        {
                            if (node is BaseNodeView nodeView && nodeView.state != null)
                            {
                                // 确保所有节点状态都被标记为已修改
                                EditorUtility.SetDirty(nodeView.state);
                            }
                        }
                        
                        // 保存所有更改
                        currentGraphData.SaveAllChanges();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"保存FlowChart数据时发生错误: {e.Message}");
                    }
                }
            }
        }

        public void Init()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.miracle.FlowGraph/Editor/UIBuilder/FlowChart.uxml");
            visualTree.CloneTree(rootVisualElement);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.miracle.FlowGraph/Editor/UIBuilder/FlowChart.uss");
            rootVisualElement.styleSheets.Add(styleSheet);

            //设置节点视图和Inspector视图
            flowChartView = rootVisualElement.Q<FlowChartView>();
            inspectorView = rootVisualElement.Q<InspectorView>();

            flowChartView.OnNodeSelected = OnNodeSelectionChanged;
            flowChartView.window = this;
            flowChartView.currentGraphData = currentGraphData;
            //构造节点
            flowChartView.ResetNodeView();

            //获取属性
            gameObjectTextField = rootVisualElement.Q<TextField>();

            //设置选择的ObjectField
            var objectField = rootVisualElement.Q<ObjectField>();
            objectField.objectType = typeof(FlowGraphData);
            objectField.value = currentGraphData;
            FlowGraphData flowGraphData = null;

            if (objectField.value != null)
            {
                gameObjectTextField.SetEnabled(true);
                gameObjectTextField.value = objectField.value.name;
            }
            else
            {
                gameObjectTextField.SetEnabled(false);
                gameObjectTextField.value = "";
            }

            objectField.RegisterCallback<ChangeEvent<UnityEngine.Object>>((evt) =>
            {
                if (objectField.value != null)
                {
                    gameObjectTextField.SetEnabled(true);
                    gameObjectTextField.value = objectField.value.name;
                }
                else
                {
                    gameObjectTextField.SetEnabled(false);
                    gameObjectTextField.value = "";
                }

                if (evt.newValue is FlowGraphData data)
                {
                    flowChartView.currentGraphData = data;
                }

                flowChartView.window = this;

                //重新选择Selection
                if (objectField.value != null)
                {
                    Selection.activeObject = evt.newValue as FlowGraphData;
                }

                //构造节点
                flowChartView.ResetNodeView();
            });

            //新建FlowChart按钮
            var button = rootVisualElement.Q<Button>("new");
            button.clicked += delegate() { objectField.value = FlowGraphDataEditor.CreateFlowGraphData(); };

            //新建刷新按钮
            var buttonR = rootVisualElement.Q<Button>("refresh");
            buttonR.clicked += delegate() { flowChartView.ResetNodeView(); };

            //关联GO名称
            gameObjectTextField.RegisterValueChangedCallback(evt =>
            {
                if (flowGraphData != null)
                {
                    flowGraphData.name = evt.newValue;
                }
            });
        }

        void OnNodeSelectionChanged(BaseNodeView nodeView)
        {
            //Debug.Log("Editor受到节点被选中信息");
            inspectorView.UpdateSelection(nodeView);
        }

        private void OnAfterAssemblyReload()
        {
            // 编译完成后重新打开窗口
            if (currentGraphData != null)
            {
                // 使用延迟调用，确保Unity编辑器已完全初始化
                EditorApplication.delayCall += () => {
                    OpenWindow(currentGraphData);
                };
            }
        }
    }
}