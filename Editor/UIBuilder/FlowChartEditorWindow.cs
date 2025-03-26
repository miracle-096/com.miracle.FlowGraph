using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FlowGraph.Node
{
    public class FlowChartEditorWindow : EditorWindow
    {
        public FlowGraphData currentGraphData;

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
    }
}