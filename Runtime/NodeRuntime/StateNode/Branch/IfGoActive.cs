using System.Collections.Generic;
using UnityEngine;

namespace FlowGraph.Node
{
    public class IfGoActive : BaseBranch
    {
        [Header("IfGoActive")]
        public List<GameObject> goActive;
        public List<GameObject> goDisActive;

        public override bool IfResult(BaseTrigger emitTrigger)
        {
            foreach(GameObject go in goActive)
            {
                if(!go.activeInHierarchy)
                    return false;
            }
            foreach (GameObject go in goDisActive)
            {
                if (go.activeInHierarchy)
                    return false;
            }
            return true;  
        }
    }

}