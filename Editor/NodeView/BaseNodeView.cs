using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using UnityEditor;

namespace FlowGraph.Node
{
    public abstract class BaseNodeView : UnityEditor.Experimental.GraphView.Node
    {
        /// <summary>
        /// 点击该节点时被调用的事件，比如转发该节点信息到Inspector中显示
        /// </summary>
        public Action<BaseNodeView> OnNodeSelected;

        public TextField textField;
        public string GUID;

        // 端口映射
        protected Dictionary<string, Port> portMap = new Dictionary<string, Port>();

        public BaseNodeView() : base()
        {
            textField = new TextField();
            GUID = Guid.NewGuid().ToString();
        }

        // 为节点n创建input port或者output port
        // Direction: 是一个简单的枚举，分为Input和Output两种
        public Port GetPortForNode(BaseNodeView n, Direction portDir, Port.Capacity capacity = Port.Capacity.Single)
        {
            // Orientation也是个简单的枚举，分为Horizontal和Vertical两种，port的数据类型是bool
            return n.InstantiatePort(Orientation.Horizontal, portDir, capacity, typeof(bool));
        }

        // 创建端口并指定类型
        public Port GetPortForNode(BaseNodeView n, Direction portDir, Type type, Port.Capacity capacity = Port.Capacity.Single)
        {
            return n.InstantiatePort(Orientation.Horizontal, portDir, capacity, type);
        }

        //告诉Inspector去绘制该节点
        public override void OnSelected()
        {
            base.OnSelected();
            Debug.Log($"{this.name}节点被点击");
            OnNodeSelected?.Invoke(this);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
        }
        public abstract NodeState state { get; set; }

        public virtual void OnEdgeRemove(Edge edge)
        {
            // 断开节点连接关系
            if (edge.output != null && edge.input != null)
            {
                var outputNodeView = edge.output.node as BaseNodeView;
                var inputNodeView = edge.input.node as BaseNodeView;
                
                if (outputNodeView != null && inputNodeView != null)
                {
                    Debug.Log($"断开连接: {outputNodeView.title}({edge.output.portName}) -> {inputNodeView.title}({edge.input.portName})");
                    
                    // 找到对应的NodePort
                    var outputPort = outputNodeView.FindNodePort(edge.output);
                    var inputPort = inputNodeView.FindNodePort(edge.input);
                    
                    if (outputPort != null && inputPort != null)
                    {
                        Debug.Log($"找到端口: {outputPort.Name}(输出) -> {inputPort.Name}(输入)");
                        NodeState.Disconnect(outputPort, inputPort);
                        
                        // 标记节点为已修改
                        EditorUtility.SetDirty(outputNodeView.state);
                        EditorUtility.SetDirty(inputNodeView.state);
                    }
                    else
                    {
                        Debug.LogError($"找不到端口: {(outputPort == null ? "输出端口为空" : "")} {(inputPort == null ? "输入端口为空" : "")}");
                    }
                }
            }
        }

        public virtual void OnEdgeCreate(Edge edge)
        {
            // 建立节点连接关系
            if (edge.output != null && edge.input != null)
            {
                var outputNodeView = edge.output.node as BaseNodeView;
                var inputNodeView = edge.input.node as BaseNodeView;
                
                if (outputNodeView != null && inputNodeView != null)
                {
                    Debug.Log($"建立连接: {outputNodeView.title}({edge.output.portName}) -> {inputNodeView.title}({edge.input.portName})");
                    
                    // 找到对应的NodePort
                    var outputPort = outputNodeView.FindNodePort(edge.output);
                    var inputPort = inputNodeView.FindNodePort(edge.input);
                    
                    if (outputPort != null && inputPort != null)
                    {
                        Debug.Log($"找到端口: {outputPort.Name}(输出) -> {inputPort.Name}(输入)");
                        NodeState.Connect(outputPort, inputPort);
                        
                        // 标记节点为已修改
                        EditorUtility.SetDirty(outputNodeView.state);
                        EditorUtility.SetDirty(inputNodeView.state);
                    }
                    else
                    {
                        Debug.LogError($"找不到端口: {(outputPort == null ? $"{outputNodeView.state}输出端口为空" : "")} {(inputPort == null ? $"{inputNodeView.state}输入端口为空" : "")}");
                    }
                }
            }
        }

        // 根据UI端口找到对应的NodePort
        public NodePort FindNodePort(Port port)
        {
            foreach (var kvp in portMap)
            {
                if (kvp.Value == port)
                {
                    return state.GetPortById(kvp.Key);
                }
            }
            return null;
        }

        // 创建控制流端口（由子类实现）
        protected abstract void CreateControlFlowPorts();

        // 创建端口
        protected virtual void CreatePorts()
        {
            // 清空当前的所有端口
            inputContainer.Clear();
            outputContainer.Clear();
            portMap.Clear();

            if (state == null)
                return;

            // 先创建控制流端口
            CreateControlFlowPorts();

            // 再创建自定义端口
            // 注意：如果某个自定义端口与控制流端口同名，我们应该跳过它，以避免重复
            HashSet<string> existingPortNames = new HashSet<string>();
            
            // 收集已存在的端口名称
            foreach (var port in inputContainer.Children().OfType<Port>())
            {
                existingPortNames.Add($"input_{port.portName}");
            }
            
            foreach (var port in outputContainer.Children().OfType<Port>())
            {
                existingPortNames.Add($"output_{port.portName}");
            }
            
            // 创建其他UI端口
            foreach (var nodePort in state.Ports)
            {
                // 检查这个端口是否已经被创建
                string portKey = nodePort.IsOutput ? $"output_{nodePort.Name}" : $"input_{nodePort.Name}";
                if (existingPortNames.Contains(portKey))
                    continue;
                
                Direction direction = nodePort.IsOutput ? Direction.Output : Direction.Input;
                Type portType = nodePort.Type ?? typeof(bool);
                
                var port = GetPortForNode(this, direction, portType);
                port.portName = nodePort.Name;
                
                if (nodePort.IsOutput)
                {
                    outputContainer.Add(port);
                }
                else
                {
                    inputContainer.Add(port);
                }
                
                portMap[nodePort.ID] = port;
            }
            
            RefreshExpandedState();
            RefreshPorts();
        }
    }


    public abstract class BaseNodeView<State> : BaseNodeView where State : NodeState
    {
        /// <summary>
        /// 关联的State
        /// </summary>
        private State _state;

        public override NodeState state
        {
            get => _state;
            set
            {
                if (_state != null)
                    _state.node = null;
                _state = (State)value;
                if (_state != null)
                {
                    _state.node = this;
                    title = _state.name;
                    // 创建端口
                    CreatePorts();
                    UpdateTitleColor();
                }
            }
        }
        
        private void UpdateTitleColor()
        {
            if (_state == null) return;

            Color runningColor = new Color(0.37f, 1, 1, 1f); //浅蓝
            Color completedColor = new Color(0.5f, 1, 0.37f, 1f); //浅绿

            switch (_state.State)
            {
                case EState.Running:
                case EState.Enter:
                case EState.Exit:
                    titleContainer.style.backgroundColor = new StyleColor(runningColor);
                    break;
                case EState.Finish:
                    titleContainer.style.backgroundColor = new StyleColor(completedColor);
                    break;
                default:
                    titleContainer.style.backgroundColor = StyleKeyword.Null;
                    break;
            }
        }
    }
}