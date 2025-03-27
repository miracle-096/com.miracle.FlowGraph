using Sirenix.OdinInspector;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    public abstract class BaseAction : NodeState
    {
        [Header("进入时等待一帧")] public bool wait1Frame = false;

        public abstract UniTask RunningLogicAsync();

        [Button("执行")]
        public override async UniTask ExecuteAsync()
        {
            TransitionState(EState.Running);

            // 在执行节点逻辑前，确保数据已经通过端口传递
            EnsureDataTransfer();

            if (wait1Frame)
            {
                await UniTask.NextFrame();
            }

            await RunningLogicAsync();
        }

        // 确保数据通过连接的端口传递
        private void EnsureDataTransfer()
        {
            var allPortsMap = GetAllPortsMap();
            
            // 将所有输出端口的当前值推送给连接的输入端口
            foreach (var portEntry in allPortsMap)
            {
                var port = portEntry.Value;
                if (port.IsOutput)
                {
                    // 获取连接的端口并尝试传递数据
                    var connections = port.GetConnections(allPortsMap);
                    foreach (var connectedPort in connections)
                    {
                        if (!connectedPort.IsOutput)
                        {
                            Debug.Log($"[EnsureDataTransfer] 从 {name}.{port.Name} 主动传递数据到 {connectedPort.Node.name}.{connectedPort.Name}");
                            
                            // 获取输出端口的当前值
                            var portType = port.Type;
                            if (portType == typeof(int))
                            {
                                var value = port.GetValue<int>();
                                connectedPort.SetValue(value);
                                Debug.Log($"传递数据: {value} (int)");
                            }
                            else if (portType == typeof(string))
                            {
                                var value = port.GetValue<string>();
                                connectedPort.SetValue(value);
                                Debug.Log($"传递数据: {value} (string)");
                            }
                            else if (portType == typeof(float))
                            {
                                var value = port.GetValue<float>();
                                connectedPort.SetValue(value);
                                Debug.Log($"传递数据: {value} (float)");
                            }
                            else if (portType == typeof(bool))
                            {
                                var value = port.GetValue<bool>();
                                connectedPort.SetValue(value);
                                Debug.Log($"传递数据: {value} (bool)");
                            }
                            else if (portType == typeof(CustomPortNode.CustomData))
                            {
                                var value = port.GetValue<CustomPortNode.CustomData>();
                                connectedPort.SetValue(value);
                                Debug.Log($"传递数据: {(value != null ? value.name : "null")} (CustomData)");
                            }
                        }
                    }
                }
            }
        }

        public virtual async UniTask RunOverAsync()
        {
            OnExitEvent?.Invoke();
            OnExitEvent = null;

            if (nextFlow)
            {
                //继续执行下一个节点
                if (nextFlow is BaseAction nextAction)
                    await nextAction.ExecuteAsync();
                else
                    await nextFlow.ExecuteAsync();
            }
            else
            {
                TransitionState(EState.Exit);
            }

        }

        // 重写InitializePorts方法，确保有标准的控制流端口
        public override void InitializePorts()
        {
            // 清除现有端口
            Ports.Clear();
            
        }

        [HideInInspector] public event Action OnExitEvent;
    }
}