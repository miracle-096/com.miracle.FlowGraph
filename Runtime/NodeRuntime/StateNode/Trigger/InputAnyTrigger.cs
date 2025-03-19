using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FlowGraph.Node
{
    public class InputAnyTrigger : BaseTrigger
    {
        public bool allKey = false;
        public List<KeyCode> keys;

        public override void RegisterSaveTypeEvent()
        {
            EventBetter.Listen<InputAnyTrigger, InputKeyCode>(this,OnInputKeyCode);
        }

        private void OnInputKeyCode(InputKeyCode input)
        {
            if(allKey || keys.Any(key => input.keyCode == key))
                ExecuteAsync().Forget();
        }
        
        public override void DeleteSaveTypeEvent()
        {
            EventBetter.Unlisten<InputKeyCode>(this);
        }

        
    }

}