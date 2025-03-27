# FlowGraph 框架

FlowGraph是一个基于Unity的可视化节点编辑器框架，用于创建流程图和数据流图。它提供了直观的节点连接方式，支持控制流和数据流的传递，可用于游戏逻辑编程、AI行为树、对话系统等场景。

## 主要特性

- 可视化节点编辑器，拖拽式操作
- 支持控制流和数据流两种传递方式
- 自定义节点类型和端口
- 运行时数据传递与处理
- 节点组管理
- 序列化和持久化节点连接

## 节点类型

FlowGraph支持以下主要节点类型：

### 1. 基础节点类型

- **Action节点**：执行具体操作的节点，有输入和输出端口，执行完成后会流向下一个节点
- **Trigger节点**：触发事件的节点，只有输出端口，用于启动流程图
- **Branch节点**：条件分支节点，有一个输入和两个输出（true/false），根据条件决定流向
- **Sequence节点**：序列节点，可以按顺序执行多个节点

### 2. 数据节点类型

- **DataSourceNode**：数据源节点，产生数据并通过输出端口传递
- **DataSinkNode**：数据接收节点，接收数据并处理
- **CustomPortNode**：自定义端口节点，可处理多种类型的数据

## 使用方法

### 创建流程图

1. 在Unity中，选择 `Assets > Create > FlowGraph > New FlowGraph`
2. 打开FlowChart编辑器窗口
3. 右键点击空白区域，从上下文菜单中选择要创建的节点类型
4. 通过拖拽连接不同节点的端口，建立节点间的连接关系

### 创建节点连接

- **控制流连接**：连接节点的控制流入口和出口，决定执行顺序
- **数据流连接**：连接不同节点的数据端口，实现数据传递

### 运行流程图

```csharp
// 获取FlowGraphData实例
FlowGraphData graphData = GetComponent<FlowGraphData>();

// 初始化并运行流程图
graphData.Run();

// 关闭流程图，清理资源
graphData.ShutDown();
```

## 数据传递功能

FlowGraph支持在节点间传递多种类型的数据：

### 数据类型

- 基础类型：int, float, string, bool
- 自定义类型：CustomData等用户定义的数据结构

### 数据传递方式

#### 1. 通过端口设置和获取数据

```csharp
// 设置输出数据
GetPort("数字输出", true)?.SetValue(42);

// 获取输入数据
int value = GetPort("数字输入", false)?.GetValue<int>() ?? 0;
```

#### 2. 使用示例

**数据源节点**：
```csharp
// 预设输出值
GetPort("数字输出", true)?.SetValue(numberValue);
GetPort("文本输出", true)?.SetValue(textValue);
GetPort("自定义输出", true)?.SetValue(customValue);
```

**数据处理节点**：
```csharp
// 获取输入
var numInput = GetPort("数字输入", false)?.GetValue<int>() ?? 0;
var strInput = GetPort("字符串输入", false)?.GetValue<string>() ?? "";

// 处理数据
intValue = numInput * 2;
stringValue = strInput + "_处理后";

// 设置输出
GetPort("数字输出", true)?.SetValue(intValue);
GetPort("字符串输出", true)?.SetValue(stringValue);
```

**数据接收节点**：
```csharp
// 获取输入数据
var numberPort = GetPort("数字输入", false);
receivedNumber = numberPort.GetValue<int>();
```

## 节点组管理

FlowGraph支持将相关节点组织成组，方便管理复杂流程图：

1. 选择多个相关节点
2. 右键选择"创建节点组"
3. 为节点组命名

## 高级功能

### 自定义节点

1. 创建一个继承自NodeState的类：
```csharp
[NodeNote("自定义节点", "分类")]
public class MyCustomNode : BaseAction
{
    // 自定义属性
    [SerializeField]
    private float myValue = 0f;
    
    // 重写端口初始化
    public override void InitializePorts()
    {
        Ports.Clear();
        AddInputPort("input", typeof(bool));
        AddOutputPort("output", typeof(bool));
        
        // 添加自定义数据端口
        AddInputPort("myInput", typeof(float));
        AddOutputPort("myOutput", typeof(string));
    }
    
    // 重写执行逻辑
    public override async UniTask RunningLogicAsync()
    {
        // 处理逻辑
        
        await RunOverAsync();
    }
}
```

### 保存和加载流程图

FlowGraph会自动序列化节点连接，可以在编辑器和运行时保持连接状态。

## 注意事项

- 确保端口类型匹配，否则无法建立连接
- 在编辑器中修改后，记得保存资源
- 避免循环引用，可能导致死循环

## 调试支持

FlowGraph提供节点执行状态的可视化：
- 未执行：默认颜色
- 正在执行：浅蓝色
- 执行完成：浅绿色

## 许可

FlowGraph 框架遵循[LICENSE]协议。

---

通过FlowGraph框架，可以快速构建各种流程图和数据流图，简化复杂逻辑的开发。
