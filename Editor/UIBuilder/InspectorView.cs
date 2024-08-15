namespace FlowGraph.Node
{
    using UnityEditor;
    using UnityEngine.UIElements;

    public class InspectorView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<InspectorView, UxmlTraits>
        {
        }

        Editor editor;

        public InspectorView()
        {
        }

        public IMGUIContainer container;
        internal void UpdateSelection(BaseNodeView nodeView)
        {
            Clear();
            //Debug.Log("显示节点的Inspector面板");
            UnityEngine.Object.DestroyImmediate(editor);

            if (nodeView == null)
                return;

            editor = Editor.CreateEditor(nodeView.state);

            container = new IMGUIContainer(() =>
            {
                if (nodeView != null && nodeView.state != null)
                {
                    editor.OnInspectorGUI();
                }
            });
            Add(container);
        }
    }
}