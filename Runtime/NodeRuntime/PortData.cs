using System;
using UnityEngine;

namespace FlowGraph.Node
{
    [Serializable]
    public class PortData
    {
        // 数据的类型名称
        [SerializeField]
        private string _typeName;
        
        // 序列化后的数据
        [SerializeField]
        private string _serializedData;
        
        // 数据值(仅运行时使用，不序列化)
        [NonSerialized]
        private object _value;
        
        public Type DataType => !string.IsNullOrEmpty(_typeName) ? Type.GetType(_typeName) : null;
        
        // 构造函数
        public PortData(Type type)
        {
            _typeName = type.AssemblyQualifiedName;
        }
        
        // 获取值
        public T GetValue<T>()
        {
            if (_value == null && !string.IsNullOrEmpty(_serializedData))
            {
                // 如果运行时值为空但有序列化数据，尝试反序列化
                try
                {
                    _value = JsonUtility.FromJson(_serializedData, DataType);
                    Debug.Log($"从序列化数据恢复值: {_serializedData}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"反序列化端口数据失败: {e.Message}");
                }
            }
            
            if (_value != null)
            {
                if (_value is T typedValue)
                {
                    return typedValue;
                }
                else
                {
                    Debug.LogWarning($"类型不匹配: 存储的值类型为 {_value.GetType().Name}，请求的类型为 {typeof(T).Name}");
                }
            }
            else
            {
                Debug.Log($"端口值为空，返回默认值");
            }
            
            return default;
        }
        
        // 设置值
        public void SetValue(object value)
        {
            _value = value;
            
            // 当设置值时也更新序列化数据(如果值可序列化)
            if (value != null)
            {
                try
                {
                    _serializedData = JsonUtility.ToJson(value);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"序列化端口数据失败: {e.Message}");
                }
            }
        }
    }
} 