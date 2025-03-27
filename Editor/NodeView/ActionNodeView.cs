using UnityEditor.Experimental.GraphView;

namespace FlowGraph.Node
{
    public class ActionNodeView : BaseNodeView<BaseAction>
    {
        // 端口引用
        protected Port inputPort;
        protected Port outputPort;

        public ActionNodeView()
        {
            title = state != null ? state.name : "ActionNode";
        }

        // 实现抽象方法，创建流程控制端口
        protected override void CreateControlFlowPorts()
        {
            // 创建标准的输入和输出控制流端口
            inputPort = GetPortForNode(this, Direction.Input, Port.Capacity.Multi);
            outputPort = GetPortForNode(this, Direction.Output, Port.Capacity.Single);
            inputPort.portName = "input";
            outputPort.portName = "output";

            inputContainer.Add(inputPort);
            outputContainer.Add(outputPort);

            // 如果state存在，添加到端口映射
            if (state != null && state.Ports.Count > 0)
            {
                var inputNodePort = state.GetPort("input", false);
                var outputNodePort = state.GetPort("output", true);

                if (inputNodePort != null)
                    portMap[inputNodePort.ID] = inputPort;
                if (outputNodePort != null)
                    portMap[outputNodePort.ID] = outputPort;
            }
        }

        public override void OnEdgeCreate(Edge edge)
        {
            base.OnEdgeCreate(edge);

            // 处理控制流连接
            if (edge.output == outputPort)
            {
                BaseNodeView targetView = edge.input.node as BaseNodeView;
                state.nextFlow = targetView.state;
            }
        }

        public override void OnEdgeRemove(Edge edge)
        {
            base.OnEdgeRemove(edge);

            // 处理控制流断开
            if (edge.output == outputPort)
            {
                state.nextFlow = null;
            }
        }
    }
}
