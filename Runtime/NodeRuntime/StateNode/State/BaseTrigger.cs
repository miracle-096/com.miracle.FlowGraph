using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    public enum ExecutePeriod
    {
        None,
        Awake,
        Enable,
        Start,
        Update,
        DisEnable,
        Destroy,
    }

    public interface ITriggerEvent
    {
        void RegisterSaveTypeEvent();
        void DeleteSaveTypeEvent();
    }

    public abstract class BaseTrigger : MonoState, ITriggerEvent
    {
        [LabelText("运行中可再次被激活")] public bool canExecuteOnRunning = false;
        [LabelText("只执行一次")] public bool runOnlyOnce = false;

        //(可选)在子类中实现下面两个方法
        public virtual void RegisterSaveTypeEvent()
        {
        }

        public virtual void DeleteSaveTypeEvent()
        {
        }

        [Button]
        public void Excute()
        {
            ExecuteAsync().Forget();
        }
        public override async UniTask ExecuteAsync()
        {
            if (!UnityEngine.Application.isPlaying)
                return;
            if (!canExecuteOnRunning)
                if (state == EState.Enter || state == EState.Running || state == EState.Exit)
                    return;

            await base.ExecuteAsync();

            if (runOnlyOnce)
                DeleteSaveTypeEvent();
            else
            {
                TransitionState(EState.Exit);
            }
        }
        
        public override async void OnEnter()
        {
            base.OnEnter();

            if (nextFlow != null)
            {
                if (nextFlow is BaseAction nextAction)
                    await nextAction.ExecuteAsync();
                else
                    await nextFlow.ExecuteAsync();
            }
                
        }
        
    }
}