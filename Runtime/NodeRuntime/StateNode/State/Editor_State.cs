using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FlowGraph.Node
{
#if UNITY_EDITOR
    public abstract partial class NodeState : ScriptableObject
    {
        [HideInInspector]
        public Vector2 nodePos; //GraphView使用

        [LabelText("节点注释"), OnValueChanged(nameof(UpdateNodeName))]
        public string explanatoryNote = "";

        public UnityEditor.Experimental.GraphView.Node node;

        private void UpdateNodeName()
        {
            node.title = GetNodeName();
            name = node.title;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public string GetNodeName()
        {
            if (!string.IsNullOrWhiteSpace(explanatoryNote))
                return explanatoryNote;
            
            string ret = GetType().Name;
            if (GetType().IsDefined(typeof(NodeNoteAttribute), true))
                ret += "\n" + (System.Attribute.GetCustomAttribute(GetType(), typeof(NodeNoteAttribute)) as NodeNoteAttribute).note;
            return ret;
        }
    }

#endif
}
