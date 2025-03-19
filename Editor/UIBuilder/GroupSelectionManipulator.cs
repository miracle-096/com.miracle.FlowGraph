using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace FlowGraph.Node
{
    public class GroupSelectionManipulator : Manipulator
    {
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.G && evt.ctrlKey)
            {
                var graphView = target as FlowChartView;
                if (graphView != null)
                {
                    graphView.CreateGroup();
                }
            }
        }
    }
} 