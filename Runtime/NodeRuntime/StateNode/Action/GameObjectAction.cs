using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    public class GameObjectAction : BaseAction
    {
        [Header("GameObjectAction")]
        public List<ActiveGo> activeGoes;

        public override async UniTask RunningLogicAsync()
        {
            foreach (var activeGO in activeGoes)
            {
                if (activeGO.go != null)
                {
                    activeGO.go.SetActive(activeGO.isActive);
                    if (activeGO.isDestroy)
                    {
                        GameObject.Destroy(activeGO.go);
                    }
                }
            }

            await RunOverAsync();
        }

        [Serializable]
        public class ActiveGo
        {
            public GameObject go;
            public bool isActive;
            public bool isDestroy = false;
        }
    }
}
