using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    public class UnityEventAction : BaseAction
    {
        [Header("UnityEventAction Action")]
        public UnityEvent unityEvent;

        public override async UniTask RunningLogicAsync()
        {
            unityEvent?.Invoke();

            await RunOverAsync();
        }
    }
}