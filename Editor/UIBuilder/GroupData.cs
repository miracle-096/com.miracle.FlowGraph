using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlowGraph.Node
{
    [Serializable]
    public class GroupData
    {
        public string title;
        public Vector2 position;
        public List<string> nodeGuids = new List<string>();
    }
} 