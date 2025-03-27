using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FlowGraph.Node
{
    [Serializable]
    public class NodePort
    {
        // 端口唯一ID
        [SerializeField]
        private string _id;
        public string ID => _id;

        // 端口名称
        [SerializeField]
        private string _name;
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        // 端口类型 (Input 或 Output)
        [SerializeField]
        private bool _isOutput;
        public bool IsOutput => _isOutput;

        // 端口所属节点
        [SerializeField]
        private NodeState _node;
        public NodeState Node => _node;

        // 连接到的目标端口ID集合
        [SerializeField]
        private List<string> _connections = new List<string>();
        public List<string> Connections => _connections;

        // 端口类型
        [SerializeField]
        private string _type;
        public Type Type => Type.GetType(_type);
        
        // 端口数据
        [SerializeField]
        private PortData _data;

        // 构造函数
        public NodePort(string name, bool isOutput, NodeState node, Type type)
        {
            _id = Guid.NewGuid().ToString();
            _name = name;
            _isOutput = isOutput;
            _node = node;
            _type = type.AssemblyQualifiedName;
            _data = new PortData(type);
        }

        // 添加连接
        public void Connect(NodePort other)
        {
            Debug.Log($"尝试连接端口: {_node.name}.{_name} -> {other.Node.name}.{other.Name}");
            
            // 确保连接列表已初始化
            if (_connections == null)
                _connections = new List<string>();
            if (other._connections == null)
                other._connections = new List<string>();
                
            if (_isOutput && !other._isOutput)
            {
                if (!_connections.Contains(other.ID))
                {
                    _connections.Add(other.ID);
                    Debug.Log($"添加连接: 输出端口 {_node.name}.{_name} -> 输入端口 {other.Node.name}.{other.Name}");
                }
                if (!other._connections.Contains(ID))
                {
                    other._connections.Add(ID);
                    Debug.Log($"添加反向连接: 输入端口 {other.Node.name}.{other.Name} <- 输出端口 {_node.name}.{_name}");
                }
            }
            else if (!_isOutput && other._isOutput)
            {
                if (!_connections.Contains(other.ID))
                {
                    _connections.Add(other.ID);
                    Debug.Log($"添加连接: 输入端口 {_node.name}.{_name} <- 输出端口 {other.Node.name}.{other.Name}");
                }
                if (!other._connections.Contains(ID))
                {
                    other._connections.Add(ID);
                    Debug.Log($"添加反向连接: 输出端口 {other.Node.name}.{other.Name} -> 输入端口 {_node.name}.{_name}");
                }
            }
            else
            {
                Debug.LogWarning($"无法连接端口: 端口类型不匹配 ({(_isOutput ? "输出" : "输入")} -> {(other._isOutput ? "输出" : "输入")})");
                return;
            }
            
            // 使用SerializedObject确保连接列表被正确序列化
            #if UNITY_EDITOR
            // 设置节点及其所有字段为脏状态
            var serializedObj = new SerializedObject(_node);
            var portsProperty = serializedObj.FindProperty("_ports");
            
            // 遍历查找当前端口
            for (int i = 0; i < portsProperty.arraySize; i++)
            {
                var portProperty = portsProperty.GetArrayElementAtIndex(i);
                var idProperty = portProperty.FindPropertyRelative("_id");
                
                if (idProperty.stringValue == _id)
                {
                    // 找到当前端口，修改其连接列表
                    var connectionsProperty = portProperty.FindPropertyRelative("_connections");
                    
                    // 清空并重新添加所有连接
                    connectionsProperty.ClearArray();
                    for (int j = 0; j < _connections.Count; j++)
                    {
                        connectionsProperty.InsertArrayElementAtIndex(j);
                        connectionsProperty.GetArrayElementAtIndex(j).stringValue = _connections[j];
                    }
                    
                    break;
                }
            }
            
            // 应用修改
            serializedObj.ApplyModifiedProperties();
            
            // 对另一个端口做同样的操作
            var otherSerializedObj = new SerializedObject(other.Node);
            var otherPortsProperty = otherSerializedObj.FindProperty("_ports");
            
            for (int i = 0; i < otherPortsProperty.arraySize; i++)
            {
                var portProperty = otherPortsProperty.GetArrayElementAtIndex(i);
                var idProperty = portProperty.FindPropertyRelative("_id");
                
                if (idProperty.stringValue == other._id)
                {
                    var connectionsProperty = portProperty.FindPropertyRelative("_connections");
                    
                    connectionsProperty.ClearArray();
                    for (int j = 0; j < other._connections.Count; j++)
                    {
                        connectionsProperty.InsertArrayElementAtIndex(j);
                        connectionsProperty.GetArrayElementAtIndex(j).stringValue = other._connections[j];
                    }
                    
                    break;
                }
            }
            
            otherSerializedObj.ApplyModifiedProperties();
            
            // 保存所有更改
            EditorUtility.SetDirty(_node);
            EditorUtility.SetDirty(other.Node);
            
            // 在编辑器中直接保存
            AssetDatabase.SaveAssets();
            #endif
            
            // 打印连接后的状态
            Debug.Log($"连接后: {_name} 有 {_connections.Count} 个连接, {other.Name} 有 {other._connections.Count} 个连接");
        }

        // 断开连接
        public void Disconnect(NodePort other)
        {
            _connections.Remove(other.ID);
            other._connections.Remove(ID);
            
            // 保存更改
            EditorUtility.SetDirty(_node);
            EditorUtility.SetDirty(other.Node);
            AssetDatabase.SaveAssets();
        }

        // 断开所有连接
        public void DisconnectAll()
        {
            _connections.Clear();
            EditorUtility.SetDirty(_node);
        }

        // 获取连接的端口
        public List<NodePort> GetConnections(Dictionary<string, NodePort> allPorts)
        {
            List<NodePort> result = new List<NodePort>();
            
            // 安全检查
            if (_connections == null)
                _connections = new List<string>();
                
            Debug.Log($"端口 {_node.name}.{_name} 获取连接，当前列表中有 {_connections.Count} 个");
            
            // 打印当前的allPorts字典内容
            Debug.Log($"当前可用端口字典包含 {allPorts.Count} 个端口:");
            foreach (var kvp in allPorts)
            {
                Debug.Log($"  可用端口 ID: {kvp.Key}, 名称: {kvp.Value.Name}, 节点: {kvp.Value.Node.name}");
            }
            
            foreach (string id in _connections)
            {
                Debug.Log($"尝试查找连接 ID: {id}");
                if (allPorts.TryGetValue(id, out NodePort port))
                {
                    Debug.Log($"找到连接: {port.Node.name}.{port.Name}");
                    result.Add(port);
                }
                else
                {
                    Debug.LogWarning($"找不到连接 ID: {id}，检查是否正确注册到端口映射");
                    
                    // 尝试在所有节点的所有端口中查找，进行全局搜索
                    bool found = false;
                    foreach (var nodeState in Resources.FindObjectsOfTypeAll<NodeState>())
                    {
                        foreach (var nodePort in nodeState.Ports)
                        {
                            if (nodePort.ID == id)
                            {
                                Debug.Log($"全局搜索找到端口: {nodeState.name}.{nodePort.Name}");
                                result.Add(nodePort);
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                    
                    if (!found)
                    {
                        Debug.LogError($"无法在任何地方找到ID为{id}的端口，可能是端口已被删除或ID无效");
                    }
                }
            }
            return result;
        }
        
        // 设置端口值
        public void SetValue(object value)
        {
            if (_data == null)
            {
                _data = new PortData(Type.GetType(_type));
            }
            
            // 添加日志记录设置值的过程
            Debug.Log($"设置端口值: 节点={_node.name}, 端口={_name}, 值={value}");
            
            _data.SetValue(value);
            
            // 如果是输出端口，立即传递值到所有连接的输入端口
            if (_isOutput)
            {
                var allPorts = _node.GetAllPortsMap();
                var connections = GetConnections(allPorts);
                
                Debug.Log($"端口 {_name} 有 {connections.Count} 个连接");
                
                foreach (var connectedPort in connections)
                {
                    if (!connectedPort.IsOutput)
                    {
                        Debug.Log($"传递值到: 节点={connectedPort.Node.name}, 端口={connectedPort.Name}");
                        connectedPort.SetValue(value);
                    }
                }
            }
        }
        
        // 获取端口值
        public T GetValue<T>()
        {
            if (_data == null)
            {
                _data = new PortData(Type.GetType(_type));
            }
            
            T result = _data.GetValue<T>();
            
            // 如果是输入端口，并且没有值或值为默认值，尝试从连接的输出端口获取
            if (!_isOutput && (result == null || EqualityComparer<T>.Default.Equals(result, default(T))))
            {
                var allPorts = _node.GetAllPortsMap();
                var connections = GetConnections(allPorts);
                
                Debug.Log($"输入端口 {_node.name}.{_name} 尝试从 {connections.Count} 个连接获取值");
                
                foreach (var connectedPort in connections)
                {
                    if (connectedPort.IsOutput)
                    {
                        Debug.Log($"从输出端口获取值: {connectedPort.Node.name}.{connectedPort.Name}");
                        T value = connectedPort._data.GetValue<T>();
                        
                        if (value != null && !EqualityComparer<T>.Default.Equals(value, default(T)))
                        {
                            Debug.Log($"获取到值: {value}");
                            _data.SetValue(value);
                            result = value;
                            break;
                        }
                    }
                }
            }
            
            return result;
        }
    }
} 