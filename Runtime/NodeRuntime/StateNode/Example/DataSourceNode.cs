using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    [NodeNote("数据源节点", "UnityBase")]
    public class DataSourceNode : BaseAction
    {
        [SerializeField]
        private int numberValue = 42;
        
        [SerializeField]
        private string textValue = "Hello FlowGraph";
        
        [SerializeField]
        private CustomPortNode.CustomData customValue = new CustomPortNode.CustomData
        {
            name = "初始数据",
            value = 100
        };
        
        public override void InitializePorts()
        {
            // 清除默认端口
            Ports.Clear();
            
            // 添加标准控制流端口
            AddInputPort("input", typeof(bool));
            AddOutputPort("output", typeof(bool));
            
            // 添加数据输出端口
            AddOutputPort("数字输出", typeof(int));
            AddOutputPort("文本输出", typeof(string));
            AddOutputPort("自定义输出", typeof(CustomPortNode.CustomData));
            
            // 预先设置输出值
            SetInitialValues();
        }
        
        // 预先设置输出端口的值
        private void SetInitialValues()
        {
            Debug.Log($"DataSourceNode正在预设值: 数字={numberValue}, 文本={textValue}");
            
            // 打印当前端口连接信息
            PrintPortConnectionInfo();
            
            // 立即设置初始值到端口
            GetPort("数字输出", true)?.SetValue(numberValue);
            GetPort("文本输出", true)?.SetValue(textValue);
            GetPort("自定义输出", true)?.SetValue(customValue);
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
                if (port.IsOutput)
                {
                    var connections = port.GetConnections(allPortsMap);
                    Debug.Log($"[连接检查] 端口 {port.Name} ({port.ID}) 有 {connections.Count} 个连接，连接列表有 {port.Connections.Count} 项");
                    
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
                    }
                }
            }
        }
        
        public override async UniTask RunningLogicAsync()
        {
            // 先检查端口连接情况
            Debug.Log($"[{name}] 执行前检查端口连接");
            PrintPortConnectionInfo();
            
            // 将数据设置到输出端口
            GetPort("数字输出", true)?.SetValue(numberValue);
            GetPort("文本输出", true)?.SetValue(textValue);
            GetPort("自定义输出", true)?.SetValue(customValue);
            
            Debug.Log($"DataSourceNode输出数据: 数字={numberValue}, 文本={textValue}, 自定义对象={customValue.name}");
            
            // 再次检查数据传递后的状态
            Debug.Log($"[{name}] 数据设置后再次检查");
            PrintPortConnectionInfo();
            
            // 等待处理完成
            await UniTask.Delay(300);
            
            await RunOverAsync();
        }
    }
} 