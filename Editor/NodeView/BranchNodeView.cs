using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;

namespace FlowGraph.Node
{
    public class BranchNodeView : BaseNodeView<BaseBranch>
    {
        // 端口引用
        protected Port inputPort;
        protected Port trueOutputPort;
        protected Port falseOutputPort;

        public BranchNodeView()
        {
            title = state != null ? state.name : "IfNode";
        }

        // 实现抽象方法，创建流程控制端口
        protected override void CreateControlFlowPorts()
        {
            //Branch有两个输出端口一个输入端口
            inputPort = GetPortForNode(this, Direction.Input, Port.Capacity.Multi);
            trueOutputPort = GetPortForNode(this, Direction.Output, Port.Capacity.Single);
            falseOutputPort = GetPortForNode(this, Direction.Output, Port.Capacity.Single);
            inputPort.portName = "input";
            trueOutputPort.portName = "true";
            falseOutputPort.portName = "false";

            inputContainer.Add(inputPort);
            outputContainer.Add(trueOutputPort);
            outputContainer.Add(falseOutputPort);

            // 如果state存在，添加到端口映射
            if (state != null && state.Ports.Count > 0)
            {
                var inputNodePort = state.GetPort("input", false);
                var trueNodePort = state.GetPort("true", true);
                var falseNodePort = state.GetPort("false", true);

                if (inputNodePort != null)
                    portMap[inputNodePort.ID] = inputPort;
                if (trueNodePort != null)
                    portMap[trueNodePort.ID] = trueOutputPort;
                if (falseNodePort != null)
                    portMap[falseNodePort.ID] = falseOutputPort;
            }
        }

        public override void OnEdgeCreate(Edge edge)
        {
            base.OnEdgeCreate(edge);

            BaseNodeView parentView = edge.output.node as BaseNodeView; //自己
            BaseNodeView childView = edge.input.node as BaseNodeView;

            if (edge.output == trueOutputPort)
            {
                (parentView.state as BaseBranch).trueFlow = childView.state;
            }
            if (edge.output == falseOutputPort)
            {
                (parentView.state as BaseBranch).falseFlow = childView.state;
            }
        }

        public override void OnEdgeRemove(Edge edge)
        {
            base.OnEdgeRemove(edge);

            BaseNodeView parentView = edge.output.node as BaseNodeView; //自己
            
            if (edge.output == trueOutputPort)
            {
                (parentView.state as BaseBranch).trueFlow = null;
            }
            if (edge.output == falseOutputPort)
            {
                (parentView.state as BaseBranch).falseFlow = null;
            }
        }
    }
}
