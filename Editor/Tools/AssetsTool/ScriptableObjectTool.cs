using UnityEditor;
using UnityEngine;

namespace FlowGraph.Editor
{
    public static class ScriptableObjectTool
    {
        public static T CreateSubAssetsIn<T>(this ScriptableObject so,string name = "")
            where T : ScriptableObject
        {
            var soData = ScriptableObject.CreateInstance<T>();
            soData.name = name;
            AssetDatabase.AddObjectToAsset(soData, so);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(so));
            return soData;
        }
    }
}
