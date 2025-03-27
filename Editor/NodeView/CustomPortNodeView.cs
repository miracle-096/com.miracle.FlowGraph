using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace FlowGraph.Node
{
    public class CustomPortNodeView : ActionNodeView
    {
        // 自定义端口引用
        private Port numberInputPort;
        private Port stringInputPort;
        private Port customInputPort;
        private Port numberOutputPort;
        private Port stringOutputPort;
        private Port customOutputPort;

        public CustomPortNodeView() : base()
        {
            title = state != null ? state.name : "CustomPortNode";
        }

        // 重写CreateControlFlowPorts方法以创建所有自定义端口
        protected override void CreateControlFlowPorts()
        {
            // 先创建标准的控制流端口
            base.CreateControlFlowPorts();

            // 再创建自定义端口
            if (state == null)
                return;

            // 创建输入端口
            numberInputPort = GetPortForNode(this, Direction.Input, typeof(int));
            stringInputPort = GetPortForNode(this, Direction.Input, typeof(string));
            customInputPort = GetPortForNode(this, Direction.Input, typeof(object));
            
            numberInputPort.portName = "数字输入";
            stringInputPort.portName = "字符串输入";
            customInputPort.portName = "自定义输入";
            
            // 创建输出端口
            numberOutputPort = GetPortForNode(this, Direction.Output, typeof(int));
            stringOutputPort = GetPortForNode(this, Direction.Output, typeof(string));
            customOutputPort = GetPortForNode(this, Direction.Output, typeof(object));
            
            numberOutputPort.portName = "数字输出";
            stringOutputPort.portName = "字符串输出";
            customOutputPort.portName = "自定义输出";
            
            // 添加到容器
            inputContainer.Add(numberInputPort);
            inputContainer.Add(stringInputPort);
            inputContainer.Add(customInputPort);
            
            outputContainer.Add(numberOutputPort);
            outputContainer.Add(stringOutputPort);
            outputContainer.Add(customOutputPort);
            
            // 添加到端口映射
            if (state != null && state.Ports.Count > 0)
            {
                var numInPort = state.GetPort("数字输入", false);
                var strInPort = state.GetPort("字符串输入", false);
                var customInPort = state.GetPort("自定义输入", false);
                
                var numOutPort = state.GetPort("数字输出", true);
                var strOutPort = state.GetPort("字符串输出", true);
                var customOutPort = state.GetPort("自定义输出", true);
                
                if (numInPort != null) portMap[numInPort.ID] = numberInputPort;
                if (strInPort != null) portMap[strInPort.ID] = stringInputPort;
                if (customInPort != null) portMap[customInPort.ID] = customInputPort;
                
                if (numOutPort != null) portMap[numOutPort.ID] = numberOutputPort;
                if (strOutPort != null) portMap[strOutPort.ID] = stringOutputPort;
                if (customOutPort != null) portMap[customOutPort.ID] = customOutputPort;
            }
        }
    }
} 