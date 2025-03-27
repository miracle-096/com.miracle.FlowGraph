using Cysharp.Threading.Tasks;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace FlowGraph.Node
{
    public enum EState
    {
        [LabelText("未执行")]
        None,
        [LabelText("正在进入")]
        Enter,
        [LabelText("正在执行")]
        Running,
        [LabelText("正在退出")]
        Exit,
        [LabelText("执行完成")]
        Finish,
    }
    public interface IStateEvent
    {
        UniTask ExecuteAsync();
        void OnEnter();
        void OnRunning();
        void OnExit();
    }

    public abstract partial class NodeState : ScriptableObject, IStateEvent
    {
        //流向下一节点的流
        public NodeState nextFlow;

        [SerializeField, Space]
        protected EState state;

        public virtual EState State
        { 
            get => state;
            set
            {
                if (state != value)
                {
                    state = value;
                    UpdateNodeColor();
                }
            }
        }
        
        [TextArea,Space]
        public string note;

        // 添加自定义端口集合
        [SerializeField, HideInInspector]
        private List<NodePort> _ports = new List<NodePort>();
        public List<NodePort> Ports => _ports;

        // 端口映射表，用于快速查找
        [NonSerialized]
        private Dictionary<string, NodePort> _portMap = new Dictionary<string, NodePort>();

        // 添加输入端口
        public NodePort AddInputPort(string name, Type type)
        {
            var port = new NodePort(name, false, this, type);
            _ports.Add(port);
            _portMap[port.ID] = port;
            return port;
        }

        // 添加输出端口
        public NodePort AddOutputPort(string name, Type type)
        {
            var port = new NodePort(name, true, this, type);
            _ports.Add(port);
            _portMap[port.ID] = port;
            return port;
        }

        // 通过名称获取端口
        public NodePort GetPort(string name, bool isOutput)
        {
            return _ports.Find(p => p.Name == name && p.IsOutput == isOutput);
        }

        // 根据ID获取端口
        public NodePort GetPortById(string id)
        {
            if (_portMap.Count == 0 && _ports.Count > 0)
            {
                // 初始化端口映射表
                foreach (var p in _ports)
                {
                    _portMap[p.ID] = p;
                }
            }
            
            _portMap.TryGetValue(id, out NodePort port);
            return port;
        }

        // 连接两个端口
        public static void Connect(NodePort output, NodePort input)
        {
            if (output != null && input != null)
            {
                Debug.Log($"静态方法 Connect: {output.Node.name}.{output.Name} -> {input.Node.name}.{input.Name}");
                
                // 保存连接前的连接数
                int outputConnBefore = output.Connections.Count;
                int inputConnBefore = input.Connections.Count;
                
                // 调用连接方法
                output.Connect(input);
                
                // 验证连接是否成功
                int outputConnAfter = output.Connections.Count;
                int inputConnAfter = input.Connections.Count;
                
                Debug.Log($"连接前: 输出端口 {output.Name} 有 {outputConnBefore} 个连接, 输入端口 {input.Name} 有 {inputConnBefore} 个连接");
                Debug.Log($"连接后: 输出端口 {output.Name} 有 {outputConnAfter} 个连接, 输入端口 {input.Name} 有 {inputConnAfter} 个连接");
                
                // 确保端口被标记为已修改
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(output.Node);
                UnityEditor.EditorUtility.SetDirty(input.Node);
                UnityEditor.AssetDatabase.SaveAssets();
                #endif
            }
        }

        // 断开两个端口连接
        public static void Disconnect(NodePort port1, NodePort port2)
        {
            if (port1 != null && port2 != null)
            {
                Debug.Log($"静态方法 Disconnect: {port1.Node.name}.{port1.Name} <-> {port2.Node.name}.{port2.Name}");
                
                // 保存断开前的连接数
                int port1ConnBefore = port1.Connections.Count;
                int port2ConnBefore = port2.Connections.Count;
                
                // 调用断开方法
                port1.Disconnect(port2);
                
                // 验证断开是否成功
                int port1ConnAfter = port1.Connections.Count;
                int port2ConnAfter = port2.Connections.Count;
                
                Debug.Log($"断开前: 端口 {port1.Name} 有 {port1ConnBefore} 个连接, 端口 {port2.Name} 有 {port2ConnBefore} 个连接");
                Debug.Log($"断开后: 端口 {port1.Name} 有 {port1ConnAfter} 个连接, 端口 {port2.Name} 有 {port2ConnAfter} 个连接");
                
                // 确保端口被标记为已修改
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(port1.Node);
                UnityEditor.EditorUtility.SetDirty(port2.Node);
                UnityEditor.AssetDatabase.SaveAssets();
                #endif
            }
        }

        // 初始化端口(子类可重写此方法创建自定义端口)
        public virtual void InitializePorts()
        {
            // 默认实现：清除所有现有端口
            _ports.Clear();
            _portMap.Clear();
            
            // 根据节点类型创建默认端口
            if (this is BaseTrigger)
            {
                // Trigger 只有输出端口
                AddOutputPort("output", typeof(bool));
            }
            else if (this is BaseAction)
            {
                // Action 有输入和输出端口
                AddInputPort("input", typeof(bool));
                AddOutputPort("output", typeof(bool));
            }
            else if (this is BaseBranch)
            {
                // Branch 有一个输入和两个输出
                AddInputPort("input", typeof(bool));
                AddOutputPort("true", typeof(bool));
                AddOutputPort("false", typeof(bool));
            }
            else if (this is BaseSequence)
            {
                // Sequence 有输入和输出
                AddInputPort("input", typeof(bool));
                AddOutputPort("output", typeof(bool));
            }
        }

        // 用于获取所有端口的字典
        public Dictionary<string, NodePort> GetAllPortsMap()
        {
            return _portMap;
        }

#if UNITY_EDITOR
        private void UpdateNodeColor()
        {
            if(node != null)
            {
                Color runningColor = new Color(0.37f, 1, 1, 1f); //浅蓝
                Color compeletedColor = new Color(0.5f, 1, 0.37f, 1f); //浅绿
                Color portColor = new Color(0.41f, 0.72f, 0.72f, 1f); //灰蓝

                if (State == EState.Running || State == EState.Enter || State == EState.Exit)
                {
                    node.titleContainer.style.backgroundColor = new StyleColor(runningColor);
                }
                else if (State == EState.Finish)
                {
                    node.titleContainer.style.backgroundColor = new StyleColor(compeletedColor);
                }
                else
                {
                    node.titleContainer.style.backgroundColor = StyleKeyword.Null;
                }
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        private void OnValidate()
        {
            UpdateNodeColor();
        }
#endif

        // 在序列化之前初始化端口
        public void OnEnable()
        {
            if (_ports == null)
            {
                _ports = new List<NodePort>();
            }
            
            if (_portMap == null)
            {
                _portMap = new Dictionary<string, NodePort>();
            }
            
            // 检查序列化前端口是否已初始化
            if (_ports.Count == 0)
            {
                Debug.Log($"节点 {name} 初始化端口 (OnEnable)");
                InitializePorts();
            }
            else
            {
                // 重新构建端口映射表
                _portMap.Clear();
                
                // 检查每个端口的完整性
                for (int i = _ports.Count - 1; i >= 0; i--)
                {
                    NodePort port = _ports[i];
                    
                    // 检查端口是否有效
                    if (port == null || string.IsNullOrEmpty(port.ID))
                    {
                        Debug.LogWarning($"节点 {name} 发现无效端口，已移除");
                        _ports.RemoveAt(i);
                        continue;
                    }
                    
                    // 如果端口所属节点为空，设置为当前节点
                    if (port.Node == null)
                    {
                        Debug.LogWarning($"端口 {port.Name} 的所属节点为空，设置为 {name}");
                        // 通过反射设置 _node 字段
                        var fieldInfo = typeof(NodePort).GetField("_node", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fieldInfo != null)
                        {
                            fieldInfo.SetValue(port, this);
                        }
                    }
                    
                    // 检查端口的连接列表
                    if (port.Connections == null)
                    {
                        // 通过反射设置 _connections 字段
                        var fieldInfo = typeof(NodePort).GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fieldInfo != null)
                        {
                            Debug.LogWarning($"端口 {port.Name} 的连接列表为空，初始化新列表");
                            fieldInfo.SetValue(port, new List<string>());
                        }
                    }
                    
                    _portMap[port.ID] = port;
                    Debug.Log($"节点 {name} 重新映射端口: {port.Name} (ID: {port.ID})");
                }
                
                // 验证端口连接的有效性
                foreach (var port in _ports)
                {
                    if (port.Connections != null && port.Connections.Count > 0)
                    {
                        Debug.Log($"端口 {name}.{port.Name} 有 {port.Connections.Count} 个连接");
                    }
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        protected void TransitionState(EState _state)
        {
            State = _state;
            switch (state)
            {
                case EState.Enter:
                    OnEnter();
                    break;
                case EState.Running:
                    OnRunning();
                    break;
                case EState.Exit:
                    OnExit();
                    break;
            }
        }

        public virtual async UniTask ExecuteAsync()
        {
            TransitionState(EState.Enter);
        }
        public virtual void OnEnter()
        {
            TransitionState(EState.Running);
        }
        public virtual void OnRunning()
        {
            TransitionState(EState.Exit);
        }
        public virtual void OnExit()
        {
            TransitionState(EState.Finish);
        }
    }
}


