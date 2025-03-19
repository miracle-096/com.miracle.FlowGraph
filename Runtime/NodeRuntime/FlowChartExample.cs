using System;
using UnityEngine;

namespace FlowGraph.Node
{
    public class InputKeyCode
    {
        public KeyCode keyCode;
    }
    public class FlowChartExample : MonoBehaviour
    {
        [SerializeField] private FlowGraphData graphData;
        
        private void Awake()
        {
            graphData?.Run();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                EventBetter.Raise(new InputKeyCode(){keyCode = KeyCode.Tab});
            }
        }

        private void OnDestroy()
        {
            graphData?.ShutDown();
        }
    }
}