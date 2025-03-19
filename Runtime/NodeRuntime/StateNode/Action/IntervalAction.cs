using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    public class IntervalAction : BaseAction
    {
        [Header("等待x秒后执行下一个")]
        public float timer = 1f;

        public override async UniTask RunningLogicAsync()
        {
            if(timer <= 0)
            {
                await RunOverAsync();
                return;
            }
            await UniTask.Delay((int)(timer * 1000));
            await RunOverAsync();
        }
    }
}