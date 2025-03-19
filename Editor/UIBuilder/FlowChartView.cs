using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static EditorDelayCall;

namespace FlowGraph.Node
{
    public class FlowChartView : GraphView
    {
        public new class UxmlFactory : UxmlFactory<FlowChartView, GraphView.UxmlTraits> { }

        public Action<BaseNodeView> OnNodeSelected;
        public FlowGraphData currentGraphData;
        public FlowChartEditorWindow window;

        // 添加节点组相关的字段
        private const string GROUP_STYLE_SHEET = "Packages/com.miracle.FlowGraph/Editor/UIBuilder/FlowChart.uss";
        private const string GROUP_TITLE = "节点组";
        private const string GROUP_STYLE_NAME = "flowchart-group";

        // 添加组数据相关的字段
        private List<GroupData> groupDataList = new List<GroupData>();
        private const string GROUP_DATA_KEY = "FlowChart_GroupData";

        public FlowChartView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);

            // 添加节点组相关的操作
            this.AddManipulator(new GroupSelectionManipulator());

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(GROUP_STYLE_SHEET);
            styleSheets.Add(styleSheet);

            //当GraphView变化时，调用方法
            graphViewChanged = OnGraphViewChanged;

            //新建搜索菜单
            var menuWindowProvider = ScriptableObject.CreateInstance<SearchMenuWindowProvider>();
            menuWindowProvider.OnSelectEntryHandler = OnMenuSelectEntry;

            nodeCreationRequest += context =>
            {
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), menuWindowProvider);
            };

            // 加载保存的组数据
            LoadGroupData();
        }

        private bool listenToChange = true;

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (listenToChange == false)
                return graphViewChange;

            if (currentGraphData == null)
                return graphViewChange;

            //对于每个被移除的节点
            if (graphViewChange.elementsToRemove != null)
            {
                graphViewChange.elementsToRemove.ForEach(elem =>
                {
                    BaseNodeView BaseNodeView = elem as BaseNodeView;
                    if (BaseNodeView != null)
                    {
                        currentGraphData.RemoveNode(BaseNodeView.state);
                    }
                    Edge edge = elem as Edge;
                    if (edge != null)
                    {
                        BaseNodeView parentView = edge.output.node as BaseNodeView;
                        parentView.OnEdgeRemove(edge);
                    }
                });
            }

            //对于每个被创建的边
            if (graphViewChange.edgesToCreate != null)
            {
                graphViewChange.edgesToCreate.ForEach(edge =>
                {
                    BaseNodeView parentView = edge.output.node as BaseNodeView;
                    parentView.OnEdgeCreate(edge);
                });
            }

            //遍历节点，记录位置点
            nodes.ForEach((n) =>
            {
                BaseNodeView view = n as BaseNodeView;
                if (view != null && view.state != null)
                {
                    view.state.nodePos = view.GetPosition().position;
                }
            });

            // 保存组数据
            SaveGroupData();

            return graphViewChange;
        }

        private bool OnMenuSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            var type = searchTreeEntry.userData as Type;

            //获取鼠标位置
            var windowRoot = window.rootVisualElement;
            var windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, context.screenMousePosition - window.position.position);
            var graphMousePosition = contentViewContainer.WorldToLocal(windowMousePosition);
            CreateNode(type, graphMousePosition);

            return true;
        }

        private void CreateNode(Type type, Vector2 pos = default)
        {
            if (currentGraphData == null)
                return;

            BaseNodeView nodeView = null;
            if (type.IsSubclassOf(typeof(BaseTrigger)))
                nodeView = new TriggerNodeView();
            if (type.IsSubclassOf(typeof(BaseAction)))
                nodeView = new ActionNodeView();
            if (type.IsSubclassOf(typeof(BaseSequence)))
                nodeView = new SequenceNodeView();
            if (type.IsSubclassOf(typeof(BaseBranch)))
                nodeView = new BranchNodeView();

            if (nodeView == null)
            {
                Debug.LogError("节点未找到对应属性的NodeView");
                return;
            }

            //添加Component，关联节点
            nodeView.OnNodeSelected = OnNodeSelected;
            nodeView.state = (NodeState)ScriptableObject.CreateInstance(type);
            nodeView.state.name = type.Name;
            nodeView.SetPosition(new Rect(pos, nodeView.GetPosition().size));

            currentGraphData.AddNode(nodeView.state);
            this.AddElement(nodeView);
        }

        //重构布局
        public void ResetNodeView()
        {
            listenToChange = false;
            //移除所有节点和边
            List<GraphElement> graphElements = new List<GraphElement>();
            nodes.ForEach(x => graphElements.Add(x));
            edges.ForEach(x => graphElements.Add(x));
            for(int i = 0;i < graphElements.Count; i++)
            {
                RemoveElement(graphElements[i]);
            }
            //Inspector删除
            OnNodeSelected(null);

            listenToChange = true;

            if (currentGraphData != null)
            {
                Debug.Log("构建节点图");
                foreach (var nodeState in currentGraphData.nodes)
                {
                    CreateBaseNodeView(nodeState);
                }
            }

            if (currentGraphData != null)
            {
                Debug.Log("构建节点边的关系");
                CreateNodeEdge();
            }

            ChangeTitleColor();
        }

        //复原节点操作
        private void CreateBaseNodeView(NodeState nodeClone)
        {
            if (currentGraphData == null || nodeClone == null)
                return;

            BaseNodeView nodeView = null;
            //判断需要复原的节点
            if (nodeClone is BaseTrigger trigger)
                nodeView = new TriggerNodeView();
            if (nodeClone is BaseAction action)
                nodeView = new ActionNodeView();
            if (nodeClone is BaseSequence sequence)
                nodeView = new SequenceNodeView();
            if (nodeClone is BaseBranch branch)
                nodeView = new BranchNodeView();

            if (nodeView == null)
            {
                Debug.LogError("节点未找到对应属性的NodeView");
                return;
            }

            nodeView.OnNodeSelected = OnNodeSelected;
            nodeView.state = nodeClone;
            nodeView.SetPosition(new Rect(nodeClone.nodePos, nodeView.GetPosition().size));

            nodeView.RefreshExpandedState();
            nodeView.RefreshPorts();

            AddElement(nodeView);
        }

        //复原节点的边
        private void CreateNodeEdge()
        {
            if (currentGraphData == null)
                return;

            //这里有点像图的邻接表
            Dictionary<NodeState, BaseNodeView> map = new Dictionary<NodeState, BaseNodeView>();
            Dictionary<BaseNodeView, Port> inputPorts = new Dictionary<BaseNodeView, Port>();
            Dictionary<BaseNodeView, List<Port>> outputPorts = new Dictionary<BaseNodeView, List<Port>>();

            ports.ForEach(x =>
            {
                var y = x.node;
                var node = y as BaseNodeView;
                if (!map.ContainsKey(node.state))
                {
                    map.Add(node.state, node);
                }
                if (!inputPorts.ContainsKey(node))
                {
                    inputPorts.Add(node, x);
                }
                if (!outputPorts.ContainsKey(node))
                {
                    outputPorts.Add(node, new List<Port>());
                }
                if (x.direction == Direction.Output)
                    outputPorts[node].Add(x);
            });

            //只负责连接下面的节点
            foreach (var node in map.Keys)
            {
                if (node is BaseSequence sequence)
                {
                    Port x = outputPorts[map[sequence]][0];
                    foreach (var nextflow in sequence.nextflows)
                    {
                        Port y = inputPorts[map[nextflow]];
                        AddEdgeByPorts(x, y);
                    }
                }
                else if (node is BaseBranch branch)
                {
                    var truePorts = outputPorts[map[branch]][0].portName == "true" ? outputPorts[map[branch]][0] : outputPorts[map[branch]][1];
                    var falsePorts = outputPorts[map[branch]][0].portName == "false" ? outputPorts[map[branch]][0] : outputPorts[map[branch]][1];

                    if (branch.trueFlow != null)
                        AddEdgeByPorts(truePorts, inputPorts[map[branch.trueFlow]]);
                    if (branch.falseFlow != null)
                        AddEdgeByPorts(falsePorts, inputPorts[map[branch.falseFlow]]);
                }
                else if (node is MonoState state)
                {
                    //普通的Action或者Trigger，只处理nextFlow就好了
                    if (state.nextFlow != null)
                        AddEdgeByPorts(outputPorts[map[state]][0], inputPorts[map[state.nextFlow]]);
                }
            }
        }

        //判断每个点是否可以相连
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort =>
            endPort.direction != startPort.direction &&
            endPort.node != startPort.node).ToList();
        }

        //连接两个点
        private void AddEdgeByPorts(Port _outputPort, Port _inputPort)
        {
            if (_outputPort.node == _inputPort.node)
                return;

            Edge tempEdge = new Edge()
            {
                output = _outputPort,
                input = _inputPort
            };
            tempEdge.input.Connect(tempEdge);
            tempEdge.output.Connect(tempEdge);
            Add(tempEdge);
        }

        protected BoolClass isDuplicate = new BoolClass();

        public override void HandleEvent(EventBase evt)
        {
            base.HandleEvent(evt);

            if (evt is ValidateCommandEvent commandEvent)
            {
                Debug.Log($"Event: {commandEvent.commandName}");
                //限制一下0.2s执行一次  不然短时间会多次执行
                if (commandEvent.commandName.Equals("Paste"))
                {
                    new EditorDelayCall().CheckBoolCall(0.2f, isDuplicate,
                        OnDuplicate);
                }
            }
        }

        protected void OnDuplicate()
        {
            Debug.Log("复制节点");
            //复制节点
            var nodesDict = new Dictionary<BaseNodeView, BaseNodeView>(); //新旧Node对照

            foreach (var selectable in selection)
            {
                var offset = 1;
                if (selectable is BaseNodeView baseNodeView)
                {
                    offset++;
                    var nodeClone = ScriptableObject.CreateInstance(baseNodeView.state.GetType()) as NodeState;
                    EditorUtility.CopySerialized(baseNodeView.state, nodeClone);

                    BaseNodeView nodeView = null;
                    //判断需要复原的节点
                    if (nodeClone is BaseTrigger trigger)
                        nodeView = new TriggerNodeView();
                    if (nodeClone is BaseAction action)
                        nodeView = new ActionNodeView();
                    if (nodeClone is BaseSequence sequence)
                        nodeView = new SequenceNodeView();
                    if (nodeClone is BaseBranch branch)
                        nodeView = new BranchNodeView();

                    if (nodeView == null)
                        return;

                    //新旧节点映射
                    if (nodeView != null)
                    {
                        nodesDict.Add(baseNodeView, nodeView);
                    }

                    nodeView.OnNodeSelected = OnNodeSelected;
                    AddElement(nodeView);
                    nodeView.state = nodeClone;

                    currentGraphData.AddNode(nodeClone);

                    //调整一下流向
                    //保持原来的流向算法好难写，还是全部设置成null把
                    nodeView.state.nextFlow = null;
                    if (nodeView.state is BaseSequence sq)
                    {
                        sq.nextflows = new List<MonoState>();
                    }
                    if (nodeView.state is BaseBranch br)
                    {
                        br.trueFlow = null;
                        br.falseFlow = null;
                    }

                    //复制出来的节点位置偏移
                    nodeView.SetPosition(new Rect(baseNodeView.GetPosition().position + (Vector2.one * 30 * offset), nodeView.GetPosition().size));
                }
            }

            for (int i = selection.Count - 1; i >= 0; i--)
            {
                //取消选择
                this.RemoveFromSelection(selection[i]);
            }

            foreach (var node in nodesDict.Values)
            {
                //选择新生成的节点
                this.AddToSelection(node);
            }
        }

        protected void ChangeTitleColor()
        {
            Color runningColor = new Color(0.37f, 1,1,1f); //浅蓝
            Color compeletedColor = new Color(0.5f,1,0.37f,1f); //浅绿
            Color portColor = new Color(0.41f, 0.72f,0.72f,1f); //灰蓝

            nodes.ForEach(x =>
            {
                if(x is BaseNodeView node)
                {
                    if (node.state?.State == EState.Running || node.state?.State == EState.Enter || node.state?.State == EState.Exit)
                    {
                        node.titleContainer.style.backgroundColor = new StyleColor(runningColor);
                    }
                    if (node.state?.State == EState.Finish)
                    {
                        node.titleContainer.style.backgroundColor = new StyleColor(compeletedColor);
                    }
                }
            });
        }

        // 添加创建节点组的方法
        public void CreateGroup()
        {
            Debug.Log("尝试创建节点组");
            var selectedNodes = selection.OfType<BaseNodeView>().ToList();
            if (selectedNodes.Count == 0)
            {
                Debug.Log("没有选中的节点，无法创建组");
                return;
            }

            Debug.Log($"选中了 {selectedNodes.Count} 个节点，开始创建组");

            var group = new Group();
            group.title = GROUP_TITLE;
            
            // 确保样式表已加载
            if (styleSheets.count == 0)
            {
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(GROUP_STYLE_SHEET);
                if (styleSheet != null)
                {
                    styleSheets.Add(styleSheet);
                }
            }
            
            group.styleSheets.Add(styleSheets[0]);
            group.AddToClassList(GROUP_STYLE_NAME);

            // 计算组的初始位置（使用选中节点的中心点）
            Vector2 centerPos = Vector2.zero;
            foreach (var node in selectedNodes)
            {
                centerPos += node.GetPosition().position;
            }
            centerPos /= selectedNodes.Count;
            
            // 设置组的位置
            group.SetPosition(new Rect(centerPos, Vector2.zero));

            foreach (var node in selectedNodes)
            {
                group.AddElement(node);
            }

            AddElement(group);
            Debug.Log("节点组创建成功");
        }

        // 添加删除节点组的方法
        public void DeleteGroup(Group group)
        {
            if (group != null)
            {
                RemoveElement(group);
            }
        }

        // 添加将节点添加到组的方法
        public void AddNodeToGroup(BaseNodeView node, Group group)
        {
            if (node != null && group != null)
            {
                group.AddElement(node);
            }
        }

        // 添加从组中移除节点的方法
        public void RemoveNodeFromGroup(BaseNodeView node, Group group)
        {
            if (node != null && group != null)
            {
                group.RemoveElement(node);
            }
        }

        // 添加保存组数据的方法
        public void SaveGroupData()
        {
            groupDataList.Clear();
            var groups = this.graphElements.OfType<Group>().ToList();
            
            foreach (var group in groups)
            {
                var groupData = new GroupData
                {
                    title = group.title,
                    position = group.GetPosition().position
                };

                // 获取组内所有节点的GUID
                foreach (var element in group.containedElements)
                {
                    if (element is BaseNodeView nodeView)
                    {
                        groupData.nodeGuids.Add(nodeView.state.GetInstanceID().ToString());
                    }
                }

                groupDataList.Add(groupData);
            }

            // 将数据序列化为JSON并保存
            string json = JsonUtility.ToJson(new SerializableGroupData { groups = groupDataList });
            EditorPrefs.SetString(GROUP_DATA_KEY, json);
        }

        // 添加加载组数据的方法
        private void LoadGroupData()
        {
            string json = EditorPrefs.GetString(GROUP_DATA_KEY, "");
            if (string.IsNullOrEmpty(json)) return;

            var serializableData = JsonUtility.FromJson<SerializableGroupData>(json);
            groupDataList = serializableData.groups;

            // 在ResetNodeView后重建组
            EditorApplication.delayCall += () =>
            {
                RebuildGroups();
            };
        }

        // 添加重建组的方法
        private void RebuildGroups()
        {
            if (groupDataList == null) return;

            foreach (var groupData in groupDataList)
            {
                var group = new Group();
                group.title = groupData.title;
                group.styleSheets.Add(styleSheets[0]);
                group.AddToClassList(GROUP_STYLE_NAME);
                group.SetPosition(new Rect(groupData.position, Vector2.zero));

                // 查找并添加组内节点
                foreach (var nodeGuid in groupData.nodeGuids)
                {
                    var node = nodes.FirstOrDefault(n => 
                        n is BaseNodeView nodeView && 
                        nodeView.state.GetInstanceID().ToString() == nodeGuid) as BaseNodeView;
                    
                    if (node != null)
                    {
                        group.AddElement(node);
                    }
                }

                AddElement(group);
            }
        }

        // 添加可序列化的组数据包装类
        [Serializable]
        private class SerializableGroupData
        {
            public List<GroupData> groups = new List<GroupData>();
        }

        public void LoadGraphData(FlowGraphData graphData)
        {
            currentGraphData = graphData;
            ClearGraph();
            if (graphData != null)
            {
                LoadNodes();
                LoadEdges();
            }
        }

        private void ClearGraph()
        {
            foreach (var node in nodes.ToList())
            {
                RemoveElement(node);
            }
            foreach (var edge in edges.ToList())
            {
                RemoveElement(edge);
            }
        }

        private void LoadNodes()
        {
            foreach (var nodeData in currentGraphData.nodes)
            {
                if (nodeData == null) continue;

                var node = CreateNode(nodeData);
                if (node != null)
                {
                    node.SetPosition(new Rect(nodeData.nodePos, Vector2.zero));
                    AddElement(node);
                }
            }
        }

        private void LoadEdges()
        {
            foreach (var nodeData in currentGraphData.nodes)
            {
                if (nodeData == null) continue;

                var sourceNode = nodes.FirstOrDefault(n => (n as BaseNodeView)?.state == nodeData);
                if (sourceNode == null) continue;

                // 加载输出连接
                if (nodeData is MonoState monoState && monoState.nextFlow != null)
                {
                    var targetNode = nodes.FirstOrDefault(n => (n as BaseNodeView)?.state == monoState.nextFlow);
                    if (targetNode != null)
                    {
                        var sourcePort = sourceNode.outputContainer.Q<Port>();
                        var targetPort = targetNode.inputContainer.Q<Port>();
                        if (sourcePort != null && targetPort != null)
                        {
                            var edge = sourcePort.ConnectTo(targetPort);
                            AddElement(edge);
                        }
                    }
                }
            }
        }

        private BaseNodeView CreateNode(NodeState nodeState)
        {
            if (nodeState is BaseAction)
                return new ActionNodeView { state = nodeState };
            else if (nodeState is BaseTrigger)
                return new TriggerNodeView { state = nodeState };
            else if (nodeState is BaseSequence)
                return new SequenceNodeView { state = nodeState };
            else if (nodeState is BaseBranch)
                return new BranchNodeView { state = nodeState };

            return null;
        }

        public void SaveGraph()
        {
            if (currentGraphData == null) return;

            // 保存节点数据
            currentGraphData.nodes.Clear();
            foreach (var node in nodes)
            {
                if (node is BaseNodeView nodeView && nodeView.state != null)
                {
                    nodeView.state.nodePos = node.GetPosition().position;
                    currentGraphData.nodes.Add(nodeView.state);
                }
            }

            // 保存连接数据
            foreach (var edge in edges)
            {
                if (edge.input.node is BaseNodeView inputNode && edge.output.node is BaseNodeView outputNode)
                {
                    if (outputNode.state is MonoState monoState)
                    {
                        monoState.nextFlow = inputNode.state as MonoState;
                    }
                }
            }

            EditorUtility.SetDirty(currentGraphData);
        }
        //
        // public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        // {
        //     evt.menu.AppendAction("添加节点", (action) =>
        //     {
        //         var menuWindowProvider = ScriptableObject.CreateInstance<SearchMenuWindowProvider>();
        //         menuWindowProvider.OnSelectEntryHandler = OnMenuSelectEntry;
        //         SearchWindow.Open(new SearchWindowContext(evt.screenMousePosition), menuWindowProvider);
        //     });
        // }
    }
}
