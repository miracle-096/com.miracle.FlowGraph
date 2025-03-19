using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    public abstract class BaseSequence : BaseAction
    {
        public List<MonoState> nextflows = new List<MonoState>();
        [Header("每个行为之间是否等待x秒,输入-1时等待1帧")] public float waitTimeEachAction = 0;
        [ReadOnly] public int runningAction = 0;

        /// <summary>
        /// 向下执行所有节点
        /// </summary>
        public override async UniTask RunningLogicAsync()
        {
            if (nextflows != null && nextflows.Count > 0)
            {
                runningAction = nextflows.Count;
                await StartActionsAsync();
            }
            else
            {
                //Sequence节点输出为空，直接切换到结束状态
                await RunOverAsync();
            }
        }

        private async UniTask StartActionsAsync()
        {
            DataCache cache = new DataCache();
            cache.count = nextflows.Count;

            //继续所有节点
            foreach (var nextFlow in nextflows)
            {
                //依赖注入,当所有Action执行完成时回调Trigger
                if (nextFlow is BaseAction action)
                    action.OnExitEvent += async () =>
                    {
                        cache.count--;
                        if (cache.count == 0)
                        {
                            await RunOverAsync();
                        }
                    };

                if (nextFlow is BaseAction nextAction)
                    await nextAction.ExecuteAsync();
                else
                    await nextFlow.ExecuteAsync();

                if (waitTimeEachAction > 0)
                    await UniTask.Delay((int)(waitTimeEachAction * 1000));
                if (waitTimeEachAction == -1)
                    await UniTask.NextFrame();
            }
        }

        private class DataCache
        {
            public int count;
        }
    }
}