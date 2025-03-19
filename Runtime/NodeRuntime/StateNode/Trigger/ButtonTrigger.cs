using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace FlowGraph.Node
{
    public class ButtonTrigger : BaseTrigger
    {
        public List<Button> buttons;

        public override void RegisterSaveTypeEvent()
        {
            foreach (var btn in buttons)
                btn?.onClick.AddListener(Excute);
        }

        //Called on DisEnable
        public override void DeleteSaveTypeEvent()
        {
            foreach (var btn in buttons)
                btn?.onClick.RemoveListener(Excute);
        }

        private void Excute()
        {
            ExecuteAsync().Forget();
        }
    }
}