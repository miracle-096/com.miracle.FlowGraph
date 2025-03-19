using UnityEngine;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    public class DebugAction : BaseAction
    {
        [Header("Debug Action")]
        public string content;

        public override async UniTask RunningLogicAsync()
        {
            Debug.Log(content);

            await RunOverAsync();
        }
    }
}