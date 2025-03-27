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
        private const string STYLE_SHEET = "Packages/com.miracle.FlowGraph/Editor/UIBuilder/FlowChart.uss";

        public FlowChartView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(STYLE_SHEET);
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
                    BaseNodeView targetView = edge.input.node as BaseNodeView;
                    
                    Debug.Log($"创建边: {parentView.name} -> {targetView.name}");
                    parentView.OnEdgeCreate(edge);
                    
                    // 确保连接成功后保存图表数据
                    EditorUtility.SetDirty(parentView.state);
                    EditorUtility.SetDirty(targetView.state);
                    EditorUtility.SetDirty(currentGraphData);
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
            
            // 保存图表数据
            if (graphViewChange.edgesToCreate != null && graphViewChange.edgesToCreate.Count > 0)
            {
                Debug.Log("保存图表数据...");
                AssetDatabase.SaveAssets();
            }

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
            if (type.IsSubclassOf(typeof(BaseSequence)))
                nodeView = new SequenceNodeView();
            if (type.IsSubclassOf(typeof(BaseBranch)))
                nodeView = new BranchNodeView();
            if (type.IsSubclassOf(typeof(BaseTrigger)))
                nodeView = new TriggerNodeView();
            if (type.IsSubclassOf(typeof(BaseAction)))
                nodeView = new ActionNodeView();

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
            // 记录图表状态，用于在重置后恢复选择
            var previousSelection = selection.OfType<BaseNodeView>().Select(v => v.state).ToList();
            
            // 清除所有节点
            listenToChange = false;
            ClearGraph();
            
            if (currentGraphData == null || currentGraphData.nodes == null)
                return;
            
            // 创建所有节点
            foreach (var node in currentGraphData.nodes)
            {
                // 确保节点有端口初始化
                if (node.Ports.Count == 0)
                {
                    node.InitializePorts();
                }
                
                CreateBaseNodeView(node);
            }
            
            // 创建连接
            CreateNodeEdge();
            
            // 恢复选择
            nodes.ForEach(n =>
            {
                var nodeView = n as BaseNodeView;
                if (nodeView != null && previousSelection.Contains(nodeView.state))
                {
                    AddToSelection(nodeView);
                }
            });
            
            listenToChange = true;
            
            // 在重置完成后保存对象
            AssetDatabase.SaveAssets();
            Debug.Log("节点视图已重置，并保存资源");
        }

        //复原节点操作
        private void CreateBaseNodeView(NodeState nodeClone)
        {
            if (currentGraphData == null || nodeClone == null)
                return;

            BaseNodeView nodeView = nodeClone switch
            {
                //判断需要复原的节点
                BaseSequence sequence => new SequenceNodeView(),
                BaseBranch branch => new BranchNodeView(),
                BaseTrigger trigger => new TriggerNodeView(),
                BaseAction action => new ActionNodeView(),
                _ => null
            };

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

            // 收集所有节点和端口
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

            // 基于自定义端口系统恢复连接
            foreach (var nodeState in map.Keys)
            {
                var nodeView = map[nodeState];
                
                // 获取所有端口
                Dictionary<string, Port> viewPortMap = new Dictionary<string, Port>();
                
                // 收集节点视图中的所有端口
                foreach (var inputPort in nodeView.inputContainer.Children().OfType<Port>())
                {
                    // 找到对应的NodePort ID
                    foreach (var kvp in nodeState.GetAllPortsMap())
                    {
                        if (kvp.Value.Name == inputPort.portName && !kvp.Value.IsOutput)
                        {
                            viewPortMap[kvp.Key] = inputPort;
                            break;
                        }
                    }
                }
                
                foreach (var outputPort in nodeView.outputContainer.Children().OfType<Port>())
                {
                    // 找到对应的NodePort ID
                    foreach (var kvp in nodeState.GetAllPortsMap())
                    {
                        if (kvp.Value.Name == outputPort.portName && kvp.Value.IsOutput)
                        {
                            viewPortMap[kvp.Key] = outputPort;
                            break;
                        }
                    }
                }
                
                // 遍历所有输出端口
                foreach (var port in nodeState.Ports.Where(p => p.IsOutput))
                {
                    // 跳过没有连接的端口
                    if (port.Connections.Count == 0)
                        continue;
                        
                    // 确保我们有这个端口的视图引用
                    if (!viewPortMap.TryGetValue(port.ID, out var outputPortView))
                        continue;
                        
                    // 处理所有连接
                    foreach (var connectionId in port.Connections)
                    {
                        // 找到目标端口
                        NodePort targetPort = null;
                        BaseNodeView targetNodeView = null;
                        
                        // 遍历所有节点查找目标端口
                        foreach (var n in map.Keys)
                        {
                            targetPort = n.GetPortById(connectionId);
                            if (targetPort != null)
                            {
                                targetNodeView = map[n];
                                break;
                            }
                        }
                        
                        if (targetPort == null || targetNodeView == null)
                            continue;
                            
                        // 在目标节点视图中找到对应的UI端口
                        Port inputPortView = null;
                        foreach (var inputPort in targetNodeView.inputContainer.Children().OfType<Port>())
                        {
                            if (inputPort.portName == targetPort.Name)
                            {
                                inputPortView = inputPort;
                                break;
                            }
                        }
                        
                        if (inputPortView != null)
                        {
                            // 连接这两个端口
                            AddEdgeByPorts(outputPortView, inputPortView);
                        }
                    }
                }
            }

            // 为兼容旧代码，保留原有逻辑
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
                else if (node is NodeState state)
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

            Debug.Log($"添加边连接: {_outputPort.node.name}.{_outputPort.portName} -> {_inputPort.node.name}.{_inputPort.portName}");

            Edge tempEdge = new Edge()
            {
                output = _outputPort,
                input = _inputPort
            };
            tempEdge.input.Connect(tempEdge);
            tempEdge.output.Connect(tempEdge);
            
            // 获取节点视图并处理数据连接
            if (_outputPort.node is BaseNodeView outputNodeView && _inputPort.node is BaseNodeView inputNodeView)
            {
                // 找到对应的NodePort
                var outputPort = outputNodeView.FindNodePort(_outputPort);
                var inputPort = inputNodeView.FindNodePort(_inputPort);
                
                if (outputPort != null && inputPort != null)
                {
                    Debug.Log($"建立数据连接: {outputPort.Name} -> {inputPort.Name}");
                    
                    // 先断开可能存在的旧连接
                    NodeState.Disconnect(outputPort, inputPort);
                    
                    // 建立新连接
                    NodeState.Connect(outputPort, inputPort);
                    
                    // 立即验证连接是否成功
                    var allPorts = outputNodeView.state.GetAllPortsMap();
                    var connections = outputPort.GetConnections(allPorts);
                    Debug.Log($"连接验证: 端口 {outputPort.Name} 现在有 {connections.Count} 个连接");
                    
                    // 检查反向连接
                    allPorts = inputNodeView.state.GetAllPortsMap();
                    connections = inputPort.GetConnections(allPorts);
                    Debug.Log($"反向连接验证: 端口 {inputPort.Name} 现在有 {connections.Count} 个连接");
                    
                    // 标记节点为已修改
                    EditorUtility.SetDirty(outputNodeView.state);
                    EditorUtility.SetDirty(inputNodeView.state);
                    
                    // 立即保存
                    AssetDatabase.SaveAssets();
                    
                    // 如果有初始值，立即传递
                    if (outputPort.IsOutput)
                    {
                        // 如果有初始值，尝试立即传递
                        if (outputPort.Type == typeof(int))
                        {
                            try {
                                int value = outputPort.GetValue<int>();
                                inputPort.SetValue(value);
                                Debug.Log($"初始值传递: {value} (int)");
                            } catch (Exception e) {
                                Debug.LogWarning($"初始值传递失败: {e.Message}");
                            }
                        }
                        else if (outputPort.Type == typeof(string))
                        {
                            try {
                                string value = outputPort.GetValue<string>();
                                inputPort.SetValue(value);
                                Debug.Log($"初始值传递: {value} (string)");
                            } catch (Exception e) {
                                Debug.LogWarning($"初始值传递失败: {e.Message}");
                            }
                        }
                        else if (outputPort.Type == typeof(CustomPortNode.CustomData))
                        {
                            try {
                                var value = outputPort.GetValue<CustomPortNode.CustomData>();
                                inputPort.SetValue(value);
                                Debug.Log($"初始值传递: {(value != null ? value.name : "null")} (CustomData)");
                            } catch (Exception e) {
                                Debug.LogWarning($"初始值传递失败: {e.Message}");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError($"无法找到端口: {(outputPort == null ? "输出端口为空" : "")} {(inputPort == null ? "输入端口为空" : "")}");
                }
            }
            
            // 添加到画布
            Add(tempEdge);
            
            // 保存整个图表
            if (currentGraphData != null)
            {
                EditorUtility.SetDirty(currentGraphData);
                AssetDatabase.SaveAssets();
            }
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

                    BaseNodeView nodeView = nodeClone switch
                    {
                        //判断需要复原的节点
                        BaseSequence sequence => new SequenceNodeView(),
                        BaseBranch branch => new BranchNodeView(),
                        BaseTrigger trigger => new TriggerNodeView(),
                        BaseAction action => new ActionNodeView(),
                        _ => null
                    };

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
                        sq.nextflows = new List<NodeState>();
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
        // public void CreateGroup()
        // {
        //     Debug.Log("尝试创建节点组");
        //     var selectedNodes = selection.OfType<BaseNodeView>().ToList();
        //     if (selectedNodes.Count == 0)
        //     {
        //         Debug.Log("没有选中的节点，无法创建组");
        //         return;
        //     }
        //
        //     Debug.Log($"选中了 {selectedNodes.Count} 个节点，开始创建组");
        //
        //     var group = new Group();
        //     group.title = GROUP_TITLE;
        //     
        //     // 确保样式表已加载
        //     if (styleSheets.count == 0)
        //     {
        //         var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(GROUP_STYLE_SHEET);
        //         if (styleSheet != null)
        //         {
        //             styleSheets.Add(styleSheet);
        //         }
        //     }
        //     
        //     group.styleSheets.Add(styleSheets[0]);
        //     group.AddToClassList(GROUP_STYLE_NAME);
        //
        //     // 计算组的初始位置（使用选中节点的中心点）
        //     Vector2 centerPos = Vector2.zero;
        //     foreach (var node in selectedNodes)
        //     {
        //         centerPos += node.GetPosition().position;
        //     }
        //     centerPos /= selectedNodes.Count;
        //     
        //     // 设置组的位置
        //     group.SetPosition(new Rect(centerPos, Vector2.zero));
        //
        //     foreach (var node in selectedNodes)
        //     {
        //         group.AddElement(node);
        //     }
        //
        //     AddElement(group);
        //     Debug.Log("节点组创建成功");
        // }
        
        // 添加保存组数据的方法
        // public void SaveGroupData()
        // {
        //     groupDataList.Clear();
        //     var groups = this.graphElements.OfType<Group>().ToList();
        //     
        //     foreach (var group in groups)
        //     {
        //         var groupData = new GroupData
        //         {
        //             title = group.title,
        //             position = group.GetPosition().position
        //         };
        //
        //         // 获取组内所有节点的GUID
        //         foreach (var element in group.containedElements)
        //         {
        //             if (element is BaseNodeView nodeView)
        //             {
        //                 groupData.nodeGuids.Add(nodeView.state.GetInstanceID().ToString());
        //             }
        //         }
        //
        //         groupDataList.Add(groupData);
        //     }
        //
        //     // 将数据序列化为JSON并保存
        //     string json = JsonUtility.ToJson(new SerializableGroupData { groups = groupDataList });
        //     EditorPrefs.SetString(GROUP_DATA_KEY, json);
        // }

        // 添加加载组数据的方法
        // private void LoadGroupData()
        // {
        //     string json = EditorPrefs.GetString(GROUP_DATA_KEY, "");
        //     if (string.IsNullOrEmpty(json)) return;
        //
        //     var serializableData = JsonUtility.FromJson<SerializableGroupData>(json);
        //     groupDataList = serializableData.groups;
        //
        //     // 在ResetNodeView后重建组
        //     EditorApplication.delayCall += () =>
        //     {
        //         RebuildGroups();
        //     };
        // }

        // 添加重建组的方法
        // private void RebuildGroups()
        // {
        //     if (groupDataList == null) return;
        //
        //     foreach (var groupData in groupDataList)
        //     {
        //         var group = new Group();
        //         group.title = groupData.title;
        //         group.styleSheets.Add(styleSheets[0]);
        //         group.AddToClassList(GROUP_STYLE_NAME);
        //         group.SetPosition(new Rect(groupData.position, Vector2.zero));
        //
        //         // 查找并添加组内节点
        //         foreach (var nodeGuid in groupData.nodeGuids)
        //         {
        //             var node = nodes.FirstOrDefault(n => 
        //                 n is BaseNodeView nodeView && 
        //                 nodeView.state.GetInstanceID().ToString() == nodeGuid) as BaseNodeView;
        //             
        //             if (node != null)
        //             {
        //                 group.AddElement(node);
        //             }
        //         }
        //
        //         AddElement(group);
        //     }
        // }

        // 添加可序列化的组数据包装类
        // [Serializable]
        // private class SerializableGroupData
        // {
        //     public List<GroupData> groups = new List<GroupData>();
        // }

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
            if (currentGraphData == null || currentGraphData.nodes == null)
                return;
            
            foreach (var nodeData in currentGraphData.nodes)
            {
                if (nodeData == null) continue;
                
                // 确保节点有端口
                if (nodeData.Ports.Count == 0)
                {
                    nodeData.InitializePorts();
                }

                BaseNodeView node = null;
                
                if (nodeData is BaseSequence)
                    node = new SequenceNodeView { state = nodeData };
                else if (nodeData is BaseBranch)
                    node = new BranchNodeView { state = nodeData };
                else if (nodeData is BaseTrigger)
                    node = new TriggerNodeView { state = nodeData };
                else if (nodeData is BaseAction)
                    node = new ActionNodeView { state = nodeData };
                    
                if (node != null)
                {
                    node.OnNodeSelected = OnNodeSelected;
                    node.SetPosition(new Rect(nodeData.nodePos, Vector2.zero));
                    AddElement(node);
                }
            }
        }

        private void LoadEdges()
        {
            // 使用现有的CreateNodeEdge方法重建连接
            CreateNodeEdge();
        }
    }
}
