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
            base.InitializePorts();
            // 添加标准控制流端口
            AddInputPort("input", typeof(bool));
            AddOutputPort("output", typeof(bool));
            
            // 添加数据输入端口
            AddInputPort("数字输入", typeof(int));
            AddInputPort("文本输入", typeof(string));
            AddInputPort("自定义输入", typeof(CustomData));
        }
        
        public override async UniTask RunningLogicAsync()
        {
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
                var customData = customPort.GetValue<CustomData>();
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