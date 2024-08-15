using Sirenix.OdinInspector;
using UnityEngine;

namespace FlowGraph.Editor
{
    public interface IAssetCreator
    {
        public void Create();
    }

    public abstract class BaseAssetCreator : ScriptableObject, IAssetCreator
    {
        [FolderPath]
        public string createPath;
        [Space, TextArea,PropertyOrder(98)]
        public string createFileName;

        [Button, PropertyOrder(99)]
        public abstract void Create();

        protected bool IsEmptyVariable()
        {
            return string.IsNullOrEmpty(createPath) || string.IsNullOrEmpty(createFileName);
        }
    }
}