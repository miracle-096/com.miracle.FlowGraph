using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace FlowGraph.Node
{
    public class FlowGraphData : ScriptableObject
    {
        public List<NodeState> nodes = new List<NodeState>();
#if UNITY_EDITOR
        [TextArea] public string tips;
#endif
        public void Run()
        {
            foreach (var node in nodes)
            {
                node.State = EState.None;
                if (node is BaseTrigger trigger)
                {
                    trigger.RegisterSaveTypeEvent();
                }
            }
        }

        public void ShutDown()
        {
            foreach (var node in nodes)
            {
                if (node is BaseTrigger trigger)
                {
                    trigger.DeleteSaveTypeEvent();
                }
            }
        }
        public void AddNode(NodeState node)
        {
            if (node == null) return;

            // 将节点作为子资源保存
            AssetDatabase.AddObjectToAsset(node, this);
            AssetDatabase.SaveAssets();

            nodes.Add(node);
            EditorUtility.SetDirty(this);
        }

        public void RemoveNode(NodeState node)
        {
            if (node == null) return;

            // 从 asset 中移除节点
            AssetDatabase.RemoveObjectFromAsset(node);
            AssetDatabase.SaveAssets();

            nodes.Remove(node);
            EditorUtility.SetDirty(this);
        }

        public void ClearNodes()
        {
            // 清除所有节点资源
            foreach (var node in nodes)
            {
                if (node != null)
                {
                    AssetDatabase.RemoveObjectFromAsset(node);
                }
            }
            AssetDatabase.SaveAssets();

            nodes.Clear();
            EditorUtility.SetDirty(this);
        }

        private void OnDestroy()
        {
            // 清理所有子资源
            ClearNodes();
        }
    }
}