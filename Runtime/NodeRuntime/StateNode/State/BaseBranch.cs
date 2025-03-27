using UnityEngine;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    public abstract class BaseBranch : BaseAction
    {
        //流向下一节点的流
        [HideInInspector]
        public NodeState trueFlow;
        [HideInInspector]
        public NodeState falseFlow;

        // 重写InitializePorts方法，确保有标准的控制流端口
        public override void InitializePorts()
        {
            // 清除现有端口
            Ports.Clear();
            
            // 添加标准的输入和输出控制流端口
        }

        //在派生类中实现该逻辑
        public abstract bool IfResult();

        public override async UniTask RunningLogicAsync()
        {
            await RunOverAsync();
        }

        public override async UniTask RunOverAsync()
        {
            //判断下一节点的流向
            nextFlow = IfResult() ? trueFlow : falseFlow;

            if (nextFlow)
            {
                //继续执行下一个节点
                if (nextFlow is BaseAction nextAction)
                    await nextAction.ExecuteAsync();
                else
                    await nextFlow.ExecuteAsync();
            }
            else
            {
                TransitionState(EState.Exit);
            }
        }
    }
}


