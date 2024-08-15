using UnityEditor;
using UnityEngine;
namespace FlowGraph.Editor
{
    [CreateAssetMenu(menuName = "FlowFrame/EditorTool/PrefabCreator")]
    public class PrefabCreator : BaseAssetCreator
    {
        public GameObject prototype;

        public override void Create()
        {
            if (IsEmptyVariable() || prototype == null)
                return;

            var newGo = Instantiate(prototype);
            PrefabUtility.SaveAsPrefabAsset(newGo, createPath + "/" + createFileName + ".prefab");
            DestroyImmediate(newGo);

            AssetDatabase.Refresh();
        }
    }
}