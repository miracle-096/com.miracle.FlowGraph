using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    [NodeNote("数据接收节点", "UnityBase")]
    public class DataSinkNode : BaseAction
    {
        [SerializeField, ReadOnly]
        private int receivedNumber;
        
        [SerializeField, ReadOnly]
        private string receivedText;
        
        [SerializeField]
        private string customObjectName;
        
        [SerializeField, ReadOnly]
        private int customObjectValue;
        
        public override void InitializePorts()
        {
            // 清除默认端口
            Ports.Clear();
            
            // 添加标准控制流端口
            AddInputPort("input", typeof(bool));
            AddOutputPort("output", typeof(bool));
            
            // 添加数据输入端口
            AddInputPort("数字输入", typeof(int));
            AddInputPort("文本输入", typeof(string));
            AddInputPort("自定义输入", typeof(CustomPortNode.CustomData));
        }
        
        // 打印端口连接信息
        private void PrintPortConnectionInfo()
        {
            Debug.Log($"===== {name} 端口连接信息 =====");
            
            var allPortsMap = GetAllPortsMap();
            
            // 打印所有端口ID的映射关系
            Debug.Log("所有端口映射表:");
            foreach (var entry in allPortsMap)
            {
                Debug.Log($"  ID: {entry.Key} -> 端口: {entry.Value.Name} ({(entry.Value.IsOutput ? "输出" : "输入")})");
            }
            
            foreach (var port in Ports)
            {
                if (!port.IsOutput) // 对于DataSinkNode，关注输入端口而非输出端口
                {
                    var connections = port.GetConnections(allPortsMap);
                    Debug.Log($"[连接检查] 输入端口 {port.Name} ({port.ID}) 有 {connections.Count} 个连接，连接列表有 {port.Connections.Count} 项");
                    
                    // 打印所有连接ID
                    if (port.Connections.Count > 0)
                    {
                        Debug.Log($"  连接ID列表: {string.Join(", ", port.Connections)}");
                    }
                    
                    // 打印所有实际连接的端口
                    foreach (var connectedPort in connections)
                    {
                        Debug.Log($"  连接到: {connectedPort.Node.name}.{connectedPort.Name} (ID: {connectedPort.ID})");
                        
                        // 检查反向连接
                        bool hasReverseConnection = connectedPort.Connections.Contains(port.ID);
                        Debug.Log($"  反向连接检查: {(hasReverseConnection ? "正常" : "异常")}");
                        
                        // 检查连接的端口当前值
                        if (connectedPort.Type == typeof(int))
                        {
                            try {
                                int value = connectedPort.GetValue<int>();
                                Debug.Log($"  端口值: {value} (int)");
                            } catch (Exception e) {
                                Debug.LogWarning($"  获取值失败: {e.Message}");
                            }
                        }
                        else if (connectedPort.Type == typeof(string))
                        {
                            try {
                                string value = connectedPort.GetValue<string>();
                                Debug.Log($"  端口值: \"{value}\" (string)");
                            } catch (Exception e) {
                                Debug.LogWarning($"  获取值失败: {e.Message}");
                            }
                        }
                        else if (connectedPort.Type == typeof(CustomPortNode.CustomData))
                        {
                            try {
                                var value = connectedPort.GetValue<CustomPortNode.CustomData>();
                                Debug.Log($"  端口值: {(value != null ? value.name : "null")} (CustomData)");
                            } catch (Exception e) {
                                Debug.LogWarning($"  获取值失败: {e.Message}");
                            }
                        }
                    }
                }
            }
        }
        
        public override async UniTask RunningLogicAsync()
        {
            // 先检查端口连接情况
            Debug.Log($"[{name}] 执行前检查端口连接");
            PrintPortConnectionInfo();
            
            // 从输入端口获取数据 - 直接获取
            TryGetDataFromInputs();
            
            Debug.Log($"DataSinkNode接收到数据: 数字={receivedNumber}, 文本={receivedText}, 自定义对象名称={customObjectName}, 值={customObjectValue}");
            
            // 等待处理完成
            await UniTask.Delay(300);
            
            await RunOverAsync();
        }

        // 尝试从所有可能的方式获取输入数据
        private void TryGetDataFromInputs()
        {
            // 1. 直接从输入端口获取
            var numberPort = GetPort("数字输入", false);
            var textPort = GetPort("文本输入", false);
            var customPort = GetPort("自定义输入", false);
            
            if (numberPort != null)
            {
                Debug.Log("尝试获取数字输入...");
                receivedNumber = numberPort.GetValue<int>();
            }
            
            if (textPort != null)
            {
                Debug.Log("尝试获取文本输入...");
                receivedText = textPort.GetValue<string>();
            }
            
            if (customPort != null)
            {
                Debug.Log("尝试获取自定义输入...");
                var customData = customPort.GetValue<CustomPortNode.CustomData>();
                if (customData != null)
                {
                    customObjectName = customData.name;
                    customObjectValue = customData.value;
                }
            }
            
            // 2. 查找所有连接的节点，尝试从它们获取数据
            var allPortsMap = GetAllPortsMap();
            foreach (var portEntry in allPortsMap)
            {
                var port = portEntry.Value;
                if (!port.IsOutput)
                {
                    var connections = port.GetConnections(allPortsMap);
                    Debug.Log($"输入端口 {port.Name} 连接了 {connections.Count} 个输出端口");
                    
                    foreach (var connectedPort in connections)
                    {
                        Debug.Log($"  连接到: {connectedPort.Node.name}.{connectedPort.Name}");
                    }
                }
            }
        }
    }
} 