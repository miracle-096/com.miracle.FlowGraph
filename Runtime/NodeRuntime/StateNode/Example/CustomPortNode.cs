using System;
using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    [NodeNote("自定义端口示例", "UnityBase")]
    public class CustomPortNode : BaseAction
    {
        [SerializeField]
        private float floatValue = 0f;
        
        [SerializeField]
        private string stringValue = "";
        
        [SerializeField] 
        private int intValue = 0;
        
        // 自定义数据结构，用于在端口间传递
        [Serializable]
        public class CustomData
        {
            public string name;
            public int value;
        }

        // 重写InitializePorts方法
        public override void InitializePorts()
        {
            // 清除默认端口
            Ports.Clear();
            
            // 首先添加标准的控制流端口
            AddInputPort("input", typeof(bool));
            AddOutputPort("output", typeof(bool));
            
            // 再添加自定义端口
            AddInputPort("数字输入", typeof(int));
            AddInputPort("字符串输入", typeof(string));
            AddInputPort("自定义输入", typeof(CustomData));
            
            AddOutputPort("数字输出", typeof(int));
            AddOutputPort("字符串输出", typeof(string));
            AddOutputPort("自定义输出", typeof(CustomData));
        }

        public override async UniTask RunningLogicAsync()
        {
            // 从输入端口获取数据
            var numInput = GetPort("数字输入", false)?.GetValue<int>() ?? 0;
            var strInput = GetPort("字符串输入", false)?.GetValue<string>() ?? "";
            var customInput = GetPort("自定义输入", false)?.GetValue<CustomData>();
            
            Debug.Log($"CustomPortNode收到输入: 数字={numInput}, 字符串={strInput}, 自定义对象={customInput?.name ?? "无"}");
            
            // 处理数据
            intValue = numInput * 2;
            stringValue = strInput + "_处理后";
            
            // 创建输出数据
            var customData = new CustomData 
            { 
                name = "处理后的数据", 
                value = intValue 
            };
            
            // 设置输出端口数据
            GetPort("数字输出", true)?.SetValue(intValue);
            GetPort("字符串输出", true)?.SetValue(stringValue);
            GetPort("自定义输出", true)?.SetValue(customData);
            
            Debug.Log($"CustomPortNode处理后输出: 数字={intValue}, 字符串={stringValue}, 自定义对象={customData.name}");
            
            // 模拟处理时间
            await UniTask.Delay(500);
            
            await RunOverAsync();
        }
    }
}