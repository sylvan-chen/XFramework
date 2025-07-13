# UIManager 重新设计

## 设计理念

根据代码注释中的设计思路，UIManager 采用了以下核心设计：

### 1. 分层管理
- **UILayer**: 每个层都是独立的 Canvas，作为 UI 界面的容器
- **分层栈**: 每个层管理自己的 UI 栈，同一层只能有一个 UI 界面处于活跃状态（栈顶）
- **层级顺序**: 通过 `SortingOrder` 控制层级的渲染顺序

### 2. 栈式管理
- **层内栈**: 每个层维护自己的 UI 栈，新打开的 UI 会暂停当前栈顶 UI
- **栈顶激活**: 每层只有栈顶的 UI 界面是激活状态
- **自动恢复**: 关闭 UI 时会自动恢复栈中的下一个 UI

### 3. 跨层级隐藏
- **低层优先**: 当打开低层级界面时，会自动隐藏更高层级的界面
- **智能恢复**: 关闭低层级界面时，会自动恢复被隐藏的高层级界面
- **状态追踪**: 系统会追踪哪些界面被跨层级隐藏，确保正确恢复

## 核心组件

### UIManager
主要的UI管理器，负责：
- 管理所有 UI 层和界面
- 处理 UI 的加载、打开、关闭
- 管理层级栈和跨层级隐藏逻辑
- 提供丰富的查询和管理接口

### UILayer
UI 层组件，负责：
- 作为 UI 界面的容器
- 管理层内的 UI 界面
- 控制层级的渲染顺序
- 提供 UI 界面的基本操作

### UIFormBase
UI 界面基类，提供：
- 界面的基本生命周期管理
- 显示、隐藏、暂停、恢复状态
- 与 UIManager 的交互接口

## 主要特性

### 1. 智能栈管理
```csharp
// 打开 UI 会自动暂停当前栈顶 UI
await Global.UIManager.OpenUIFormAsync("SettingsUI", "UI");

// 关闭 UI 会自动恢复栈中的下一个 UI
Global.UIManager.CloseUIForm("SettingsUI");
```

### 2. 层级感知
```csharp
// 在不同层级打开 UI
await Global.UIManager.OpenUIFormAsync("GameUI", "Game");    // 游戏层
await Global.UIManager.OpenUIFormAsync("MenuUI", "UI");      // UI层
await Global.UIManager.OpenUIFormAsync("TipUI", "Tip");      // 提示层
```

### 3. 重复打开处理
```csharp
// 重复打开已存在的 UI 会提升到栈顶，不会重新加载
var ui = await Global.UIManager.OpenUIFormAsync("MainMenu", "UI");
var sameUI = await Global.UIManager.OpenUIFormAsync("MainMenu", "UI"); // 返回同一个实例
```

### 4. 跨层级隐藏
```csharp
// 打开低层级 UI 时，高层级 UI 会被自动隐藏
await Global.UIManager.OpenUIFormAsync("HighLevelUI", "Popup");
await Global.UIManager.OpenUIFormAsync("LowLevelUI", "UI");  // HighLevelUI 会被隐藏

// 关闭低层级 UI 时，高层级 UI 会被自动恢复
Global.UIManager.CloseUIForm("LowLevelUI");  // HighLevelUI 会被恢复
```

## API 参考

### 核心方法

#### 打开 UI
```csharp
UniTask<UIFormBase> OpenUIFormAsync(string uiFormName, string layerName = null)
```

#### 关闭 UI
```csharp
void CloseUIForm(string uiFormName, bool allowResumeNext = true)
void CloseAllUIForms()
```

#### 查询方法
```csharp
bool IsUIFormOpen(string uiFormName)
UIFormBase GetOpenUIForm(string uiFormName)
UIFormBase[] GetAllOpenUIForms()
```

#### 层级管理
```csharp
string GetTopUIFormNameInLayer(string layerName)
UIFormBase GetTopUIFormInLayer(string layerName)
string GetGlobalTopUIFormName()
UIFormBase GetGlobalTopUIForm()
```

#### 调试工具
```csharp
string GetLayerStackInfo(string layerName)
string GetAllLayerStackInfo()
int GetUIFormPositionInLayer(string uiFormName, string layerName)
bool IsUIFormOnTopOfLayer(string uiFormName, string layerName)
```

## 使用示例

### 基本使用
```csharp
// 打开主菜单
var mainMenu = await Global.UIManager.OpenUIFormAsync("MainMenu", "UI");

// 打开设置界面（会暂停主菜单）
var settings = await Global.UIManager.OpenUIFormAsync("Settings", "UI");

// 关闭设置界面（会恢复主菜单）
Global.UIManager.CloseUIForm("Settings");
```

### 层级管理
```csharp
// 在不同层级打开 UI
await Global.UIManager.OpenUIFormAsync("GameHUD", "Game");     // 游戏层
await Global.UIManager.OpenUIFormAsync("MainMenu", "UI");      // UI层
await Global.UIManager.OpenUIFormAsync("LoadingTip", "Tip");   // 提示层

// 查看层级信息
Debug.Log(Global.UIManager.GetAllLayerStackInfo());
```

### 状态查询
```csharp
// 检查 UI 是否打开
if (Global.UIManager.IsUIFormOpen("MainMenu"))
{
    Debug.Log("主菜单已打开");
}

// 获取当前全局顶层 UI
var topUI = Global.UIManager.GetGlobalTopUIForm();
if (topUI != null)
{
    Debug.Log($"当前顶层UI: {topUI.UIFormName}");
}
```

## 设计优势

1. **清晰的层级结构**: 每个层都有明确的职责和渲染顺序
2. **智能状态管理**: 自动处理 UI 的暂停、恢复和隐藏
3. **高效的资源管理**: 支持 UI 缓存和重用
4. **丰富的查询接口**: 提供全面的 UI 状态查询功能
5. **易于调试**: 提供详细的调试信息和状态追踪

## 兼容性

为了保持向后兼容，UIManager 提供了一些标记为 `Obsolete` 的方法：
- `IsUIFormActive()` → `IsUIFormOpen()`
- `GetActiveUIForm()` → `GetOpenUIForm()`
- `GetAllActiveUIForms()` → `GetAllOpenUIForms()`
- `GetTopUIFormName()` → `GetGlobalTopUIFormName()`
- `GetTopUIForm()` → `GetGlobalTopUIForm()`

这些方法在过渡期间仍然可用，但建议逐步迁移到新的 API。
- **UI分组管理**：支持将UI界面组织到不同的组中，便于管理和排序
- **UI栈管理**：自动管理UI界面的显示顺序和暂停/恢复状态
- **生命周期管理**：完整的UI界面生命周期管理（初始化、显示、隐藏、暂停、恢复、销毁）
- **事件系统**：提供UI界面状态变化的事件通知
- **自动Canvas管理**：自动创建和管理UI Canvas，支持多层级UI组织

### 2. 架构设计
- **UIManager**：UI管理器主类，负责整个UI系统的管理
- **UIGroup**：UI组类，用于管理同一层级的UI界面
- **UIFormBase**：UI界面基类，所有UI界面都应该继承此类

## 使用方法

### 1. 基本设置

UI Manager 会自动初始化，创建默认的UI组：
- Background (SortingOrder: -100)
- Default (SortingOrder: 0)
- Normal (SortingOrder: 0)
- Popup (SortingOrder: 100)
- System (SortingOrder: 200)
- Top (SortingOrder: 300)

### 2. 创建UI界面

```csharp
using UnityEngine;
using XFramework;

public class MainMenuUI : UIFormBase
{
    protected override void OnInit()
    {
        base.OnInit();
        // 初始化UI界面
        SetButtonClick("StartButton", OnStartButtonClick);
        SetButtonClick("QuitButton", OnQuitButtonClick);
    }

    protected override void OnShow()
    {
        base.OnShow();
        // 界面显示时的逻辑
    }

    protected override void OnHide()
    {
        base.OnHide();
        // 界面隐藏时的逻辑
    }

    private void OnStartButtonClick()
    {
        // 开始游戏
        Global.UIManager.OpenUIFormAsync("GameUI");
        Close(); // 关闭当前界面
    }

    private void OnQuitButtonClick()
    {
        // 退出游戏
        Application.Quit();
    }
}
```

### 3. 打开UI界面

```csharp
// 异步打开UI界面
var uiForm = await Global.UIManager.OpenUIFormAsync("MainMenuUI");

// 在指定组中打开UI界面
var popupForm = await Global.UIManager.OpenUIFormAsync("ConfirmDialog", "Popup");

// 不暂停被覆盖的UI界面
var overlayForm = await Global.UIManager.OpenUIFormAsync("LoadingUI", "System", false);
```

### 4. 关闭UI界面

```csharp
// 关闭指定UI界面
Global.UIManager.CloseUIForm("MainMenuUI");

// 关闭所有UI界面
Global.UIManager.CloseAllUIForms();

// 在UI界面内部关闭自己
Close();
```

### 5. 创建自定义UI组

```csharp
// 创建自定义UI组
var customGroup = Global.UIManager.CreateUIGroup("CustomGroup", 150);

// 在自定义组中打开UI界面
await Global.UIManager.OpenUIFormAsync("CustomUI", "CustomGroup");
```

## API 参考

### UIManager 主要方法

| 方法 | 描述 |
|------|------|
| `OpenUIFormAsync(uiFormName, groupName, pauseCoveredUIForm)` | 异步打开UI界面 |
| `CloseUIForm(uiFormName, allowResumeNext)` | 关闭UI界面 |
| `CloseAllUIForms()` | 关闭所有UI界面 |
| `CreateUIGroup(groupName, sortingOrder)` | 创建UI组 |
| `GetUIGroup(groupName)` | 获取UI组 |
| `IsUIFormActive(uiFormName)` | 检查UI界面是否激活 |
| `GetActiveUIForm(uiFormName)` | 获取激活的UI界面 |
| `GetTopUIForm()` | 获取当前顶部UI界面 |
| `UnloadUIForm(uiFormName)` | 卸载UI界面 |

### UIFormBase 主要方法

| 方法 | 描述 |
|------|------|
| `Show()` | 显示UI界面 |
| `Hide()` | 隐藏UI界面 |
| `Pause()` | 暂停UI界面 |
| `Resume()` | 恢复UI界面 |
| `Close()` | 关闭UI界面 |
| `Refresh()` | 刷新UI界面 |
| `SetText(name, text)` | 设置文本内容 |
| `SetButtonClick(name, callback)` | 设置按钮点击事件 |
| `SetImage(name, sprite)` | 设置图片精灵 |
| `FindComponent<T>(name)` | 查找子组件 |

### UIFormBase 虚方法（可重写）

| 方法 | 描述 |
|------|------|
| `OnInit()` | 初始化时调用 |
| `OnShow()` | 显示时调用 |
| `OnHide()` | 隐藏时调用 |
| `OnPause()` | 暂停时调用 |
| `OnResume()` | 恢复时调用 |
| `OnRefresh()` | 刷新时调用 |
| `OnDestroyUI()` | 销毁时调用 |
| `OnUpdate()` | 每帧更新时调用 |
| `OnLateUpdate()` | 每帧延迟更新时调用 |

## 事件系统

UI Manager 提供了以下事件：

```csharp
// 订阅UI界面事件
Global.UIManager.OnUIFormOpened += OnUIFormOpened;
Global.UIManager.OnUIFormClosed += OnUIFormClosed;
Global.UIManager.OnUIFormPaused += OnUIFormPaused;
Global.UIManager.OnUIFormResumed += OnUIFormResumed;

private void OnUIFormOpened(string uiFormName)
{
    Debug.Log($"UI Form '{uiFormName}' opened");
}

private void OnUIFormClosed(string uiFormName)
{
    Debug.Log($"UI Form '{uiFormName}' closed");
}
```

## 最佳实践

1. **UI界面命名**：使用描述性的名称，如 "MainMenuUI"、"InventoryUI"、"SettingsUI"
2. **组织UI组**：根据功能和层级组织UI界面到不同的组中
3. **生命周期管理**：正确重写UI界面的生命周期方法
4. **内存管理**：及时卸载不再使用的UI界面
5. **异步操作**：使用异步方法加载UI界面，避免阻塞主线程

## 依赖项

- Unity 2020.3 或更高版本
- UniTask（用于异步操作）
- XFramework Core（框架核心）

## 示例项目

参考 `TestUIForm.cs` 了解基本的UI界面实现。

## 注意事项

1. 所有UI界面预制体必须挂载继承自 `UIFormBase` 的脚本
2. UI界面的资源名称必须与传入的 `uiFormName` 参数一致
3. UI界面会自动添加到指定的UI组中，不需要手动设置父对象
4. 建议在UI界面的 `OnInit()` 方法中进行组件查找和事件绑定

---

*此文档基于 XFramework UI Manager v1.0*
