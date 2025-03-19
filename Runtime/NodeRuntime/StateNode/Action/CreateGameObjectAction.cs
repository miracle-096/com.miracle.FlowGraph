using UnityEngine;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    public class CreateGameObjectAction : BaseAction
    {
        [Header("CreateGameObjectAction")]
        public GameObject protype;
        public Transform startPos;
        public Transform parent;

        public override async UniTask RunningLogicAsync()
        {
            GameObject.Instantiate(protype, startPos.transform.position, Quaternion.identity, parent);

            await RunOverAsync();
        }
    }
}