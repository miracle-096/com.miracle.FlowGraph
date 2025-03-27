﻿using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace FlowGraph.Node
{
    public class TriggerNodeView : BaseNodeView<BaseTrigger>
    {
        // 端口引用
        protected Port outputPort;

        public TriggerNodeView()
        {
            title = state != null ? state.name : "TriggerNode";
        }

        // 实现抽象方法，创建流程控制端口
        protected override void CreateControlFlowPorts()
        {
            // 创建标准输出端口
            outputPort = GetPortForNode(this, Direction.Output, Port.Capacity.Single);
            outputPort.portName = "output";
            outputContainer.Add(outputPort);

            // 如果state存在，添加到端口映射
            if (state != null && state.Ports.Count > 0)
            {
                var outputNodePort = state.GetPort("output", true);
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
