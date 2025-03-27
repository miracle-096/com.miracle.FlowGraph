using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;

namespace FlowGraph.Node
{
    public class SequenceNodeView : BaseNodeView<BaseSequence>
    {
        // 端口引用
        protected Port inputPort;
        protected Port outputPort;

        public SequenceNodeView()
        {
            title = state != null ? state.name : "SequenceNode";
        }

        // 实现抽象方法，创建流程控制端口
        protected override void CreateControlFlowPorts()
        {
            //Sequence有一个输出端口一个输入端口,输入接口只能单连接，输出端口可以多连接
            inputPort = GetPortForNode(this, Direction.Input, Port.Capacity.Single);
            outputPort = GetPortForNode(this, Direction.Output, Port.Capacity.Multi);
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

            if (edge.output == outputPort)
            {
                BaseNodeView targetView = edge.input.node as BaseNodeView;
                (state as BaseSequence).TryAddNextFlows(targetView.state);
            }
        }

        public override void OnEdgeRemove(Edge edge)
        {
            base.OnEdgeRemove(edge);

            if (edge.output == outputPort)
            {
                BaseNodeView targetView = edge.input.node as BaseNodeView;
                (state as BaseSequence).nextflows.Remove(targetView.state);
            }
        }
    }
}
