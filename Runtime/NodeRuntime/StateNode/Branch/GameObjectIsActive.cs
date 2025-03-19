using UnityEngine;

namespace FlowGraph.Node
{
    public class GameObjectIsActive : BaseBranch
    {
        [Header("GameObjectIsActive")]
        public GameObject target;

        public override bool IfResult()
        {
            return target.activeSelf;
        }
    }

}