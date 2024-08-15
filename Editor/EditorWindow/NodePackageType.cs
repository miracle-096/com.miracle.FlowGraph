using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace FlowGraph.Node
{
    [CreateAssetMenu(menuName = "FlowFrame/EditorTool/NodePackageType")]
    public class NodePackageType : ScriptableObject
    {
        [System.Serializable]
        public class NodePackageTypeData
        {
            public string packageType = string.Empty;
            [FolderPath]
            public string nodeScriptsPath;
        }

        public List<NodePackageTypeData> packageTypes = new List<NodePackageTypeData>();
    
        public static List<string> GetAllTypeStr()
        {
            List<string> result = new List<string>();
            var data = AssetDatabase.LoadAssetAtPath<NodePackageType>("Packages/com.zpgame.flowgraph/Editor/EditorWindow/节点分类包和路径.asset");
            data.packageTypes.ForEach(x => result.Add(x.packageType));
            return result;
        }

        public static string GetPathByType(string nodeType)
        {
            string result = string.Empty;
            var data = AssetDatabase.LoadAssetAtPath<NodePackageType>("Packages/com.zpgame.flowgraph/Editor/EditorWindow/节点分类包和路径.asset");
            data.packageTypes.ForEach(x =>
            { 
                if(x.packageType == nodeType)
                    result = x.nodeScriptsPath;
            });
            return result;
        }

    }
}