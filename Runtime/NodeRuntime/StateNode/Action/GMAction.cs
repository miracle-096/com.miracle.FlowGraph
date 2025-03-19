using UnityEngine;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    public class GMAction : BaseAction
    {
        [Header("GMAction")]
        public bool quitGame = true;

        public override async UniTask RunningLogicAsync()
        {
            Application.Quit();

            await RunOverAsync();
        }
    }
}