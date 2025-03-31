using Sirenix.OdinInspector;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    public abstract class BaseAction : NodeState
    {
        [Header("进入时等待一帧")] public bool wait1Frame = false;

        public abstract UniTask RunningLogicAsync();

        [Button("执行")]
        public override async UniTask ExecuteAsync()
        {
            TransitionState(EState.Running);

            if (wait1Frame)
            {
                await UniTask.NextFrame();
            }

            await RunningLogicAsync();
        }

        public virtual async UniTask RunOverAsync()
        {
            OnExitEvent?.Invoke();
            OnExitEvent = null;

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

        // 重写InitializePorts方法，确保有标准的控制流端口
        public override void InitializePorts()
        {
            base.InitializePorts();
            
        }

        [HideInInspector] public event Action OnExitEvent;
    }
}