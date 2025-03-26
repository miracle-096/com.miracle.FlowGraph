using Cysharp.Threading.Tasks;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UIElements;

namespace FlowGraph.Node
{
    public enum EState
    {
        [LabelText("未执行")]
        None,
        [LabelText("正在进入")]
        Enter,
        [LabelText("正在执行")]
        Running,
        [LabelText("正在退出")]
        Exit,
        [LabelText("执行完成")]
        Finish,
    }
    public interface IStateEvent
    {
        UniTask ExecuteAsync();
        void OnEnter();
        void OnRunning();
        void OnExit();
    }

    public abstract partial class NodeState : ScriptableObject, IStateEvent
    {
        //流向下一节点的流
        public NodeState nextFlow;

        [SerializeField, Space]
        protected EState state;

        public virtual EState State
        { 
            get => state;
            set
            {
                if (state != value)
                {
                    state = value;
                    UpdateNodeColor();
                }
            }
        }
        
        [TextArea,Space]
        public string note;

#if UNITY_EDITOR
        private void UpdateNodeColor()
        {
            if(node != null)
            {
                Color runningColor = new Color(0.37f, 1, 1, 1f); //浅蓝
                Color compeletedColor = new Color(0.5f, 1, 0.37f, 1f); //浅绿
                Color portColor = new Color(0.41f, 0.72f, 0.72f, 1f); //灰蓝

                if (State == EState.Running || State == EState.Enter || State == EState.Exit)
                {
                    node.titleContainer.style.backgroundColor = new StyleColor(runningColor);
                }
                else if (State == EState.Finish)
                {
                    node.titleContainer.style.backgroundColor = new StyleColor(compeletedColor);
                }
                else
                {
                    node.titleContainer.style.backgroundColor = StyleKeyword.Null;
                }
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        private void OnValidate()
        {
            UpdateNodeColor();
        }
#endif

        protected void TransitionState(EState _state)
        {
            State = _state;
            switch (state)
            {
                case EState.Enter:
                    OnEnter();
                    break;
                case EState.Running:
                    OnRunning();
                    break;
                case EState.Exit:
                    OnExit();
                    break;
            }
        }

        public virtual async UniTask ExecuteAsync()
        {
            TransitionState(EState.Enter);
        }
        public virtual void OnEnter()
        {
            TransitionState(EState.Running);
        }
        public virtual void OnRunning()
        {
            TransitionState(EState.Exit);
        }
        public virtual void OnExit()
        {
            TransitionState(EState.Finish);
        }
    }
}


