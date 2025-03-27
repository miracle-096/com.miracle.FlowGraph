using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace FlowGraph.Node
{
    public class FlowGraphData : ScriptableObject
    {
        public List<NodeState> nodes = new List<NodeState>();
#if UNITY_EDITOR
        [TextArea] public string tips;

        // 添加一个方法用于保存所有修改
        public void SaveAllChanges()
        {
            // 确保所有节点都有端口初始化
            foreach (var node in nodes)
            {
                if (node.Ports.Count == 0)
                {
                    node.InitializePorts();
                }
            }
            
            // 标记资源为已修改
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
        public void Run()
        {
            // 首先初始化所有节点的端口连接数据
            InitializeNodeConnections();
            
            foreach (var node in nodes)
            {
                node.State = EState.None;
                if (node is BaseTrigger trigger)
                {
                    trigger.RegisterSaveTypeEvent();
                }
            }
        }

        // 初始化节点之间的数据连接
        private void InitializeNodeConnections()
        {
            Debug.Log("开始初始化节点间的数据连接...");
            
            // 创建一个统一的端口映射表，包含所有节点的端口
            Dictionary<string, NodePort> allPortsMap = new Dictionary<string, NodePort>();
            
            // 确保每个节点都初始化了其端口
            foreach (var node in nodes)
            {
                // 确保节点有端口
                if (node.Ports.Count == 0)
                {
                    Debug.Log($"节点 {node.name} 没有端口，初始化中...");
                    node.InitializePorts();
                }
                
                Debug.Log($"节点 {node.name} 有 {node.Ports.Count} 个端口");
                
                // 清除并重建端口映射，确保所有端口都被正确注册
                var nodePortMap = new Dictionary<string, NodePort>();
                foreach (var port in node.Ports)
                {
                    // 确保端口有正确的 ID
                    if (string.IsNullOrEmpty(port.ID))
                    {
                        Debug.LogError($"端口 {node.name}.{port.Name} 的ID为空！这可能导致连接失败。");
                        continue;
                    }
                    
                    nodePortMap[port.ID] = port;
                    Debug.Log($"注册端口: {node.name}.{port.Name} (ID: {port.ID})");
                }
                
                // 使用反射设置节点的 _portMap 字段
                var fieldInfo = typeof(NodeState).GetField("_portMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(node, nodePortMap);
                    Debug.Log($"成功通过反射更新节点 {node.name} 的端口映射表");
                }
                
                // 合并到全局映射
                foreach (var entry in nodePortMap)
                {
                    allPortsMap[entry.Key] = entry.Value;
                }
            }
            
            Debug.Log($"全局端口映射构建完成，共有 {allPortsMap.Count} 个端口");
            
            // 验证端口连接
            foreach (var node in nodes)
            {
                foreach (var port in node.Ports)
                {
                    if (port.Connections.Count > 0)
                    {
                        Debug.Log($"端口 {node.name}.{port.Name} 有 {port.Connections.Count} 个连接");
                        
                        // 验证每个连接
                        foreach (var connectionId in port.Connections)
                        {
                            if (allPortsMap.TryGetValue(connectionId, out NodePort connectedPort))
                            {
                                Debug.Log($"验证连接成功: {port.ID} -> {connectedPort.ID} ({node.name}.{port.Name} -> {connectedPort.Node.name}.{connectedPort.Name})");
                                
                                // 确保双向连接正确
                                if (!connectedPort.Connections.Contains(port.ID))
                                {
                                    Debug.LogWarning($"检测到单向连接，修复中: {port.ID} -> {connectedPort.ID}");
                                    connectedPort.Connections.Add(port.ID);
                                }
                            }
                            else
                            {
                                Debug.LogError($"无效连接: 端口 {node.name}.{port.Name} 连接到不存在的ID {connectionId}");
                            }
                        }
                    }
                }
            }
            
            // 检查每个节点的端口连接，执行数据传递
            foreach (var node in nodes)
            {
                foreach (var port in node.Ports)
                {
                    if (port.IsOutput)
                    {
                        // 获取连接的端口并尝试传递初始数据
                        var connections = port.GetConnections(allPortsMap);
                        Debug.Log($"节点 {node.name} 的输出端口 {port.Name} 有 {connections.Count} 个连接");
                        
                        // 对于每个连接的输入端口，尝试传递初始值
                        foreach (var connectedPort in connections)
                        {
                            if (!connectedPort.IsOutput)
                            {
                                Debug.Log($"尝试传递数据: {node.name}.{port.Name} -> {connectedPort.Node.name}.{connectedPort.Name}");
                                
                                try
                                {
                                    // 根据端口类型传递数据
                                    if (port.Type == typeof(int))
                                    {
                                        int value = port.GetValue<int>();
                                        connectedPort.SetValue(value);
                                        Debug.Log($"传递整型数据: {value}");
                                    }
                                    else if (port.Type == typeof(string))
                                    {
                                        string value = port.GetValue<string>();
                                        connectedPort.SetValue(value);
                                        Debug.Log($"传递字符串数据: {value}");
                                    }
                                    else if (port.Type == typeof(float))
                                    {
                                        float value = port.GetValue<float>();
                                        connectedPort.SetValue(value);
                                        Debug.Log($"传递浮点数据: {value}");
                                    }
                                    else if (port.Type == typeof(bool))
                                    {
                                        bool value = port.GetValue<bool>();
                                        connectedPort.SetValue(value);
                                        Debug.Log($"传递布尔数据: {value}");
                                    }
                                    else if (port.Type == typeof(CustomPortNode.CustomData))
                                    {
                                        var value = port.GetValue<CustomPortNode.CustomData>();
                                        connectedPort.SetValue(value);
                                        Debug.Log($"传递自定义数据: {(value != null ? value.name : "null")}");
                                    }
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"传递数据时发生错误: {e.Message}");
                                }
                            }
                        }
                    }
                }
            }
            
            // 保存更改
#if UNITY_EDITOR
            foreach (var node in nodes)
            {
                EditorUtility.SetDirty(node);
            }
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
            
            Debug.Log("节点连接初始化完成");
        }

        public void ShutDown()
        {
            foreach (var node in nodes)
            {
                if (node is BaseTrigger trigger)
                {
                    trigger.DeleteSaveTypeEvent();
                }
            }
        }
        public void AddNode(NodeState node)
        {
            if (node == null) return;

            // 确保节点端口已初始化
            if (node.Ports.Count == 0)
            {
                node.InitializePorts();
            }

            // 将节点作为子资源保存
            AssetDatabase.AddObjectToAsset(node, this);
            AssetDatabase.SaveAssets();

            nodes.Add(node);
            EditorUtility.SetDirty(this);
        }

        public void RemoveNode(NodeState node)
        {
            if (node == null) return;

            // 从 asset 中移除节点
            AssetDatabase.RemoveObjectFromAsset(node);
            AssetDatabase.SaveAssets();

            nodes.Remove(node);
            EditorUtility.SetDirty(this);
        }

        public void ClearNodes()
        {
            // 清除所有节点资源
            foreach (var node in nodes)
            {
                if (node != null)
                {
                    AssetDatabase.RemoveObjectFromAsset(node);
                }
            }
            AssetDatabase.SaveAssets();

            nodes.Clear();
            EditorUtility.SetDirty(this);
        }

        private void OnDestroy()
        {
            // 清理所有子资源
            ClearNodes();
        }
    }
}