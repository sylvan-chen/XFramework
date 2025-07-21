# SimpleDressup 系统完整使用指南

## 概述

SimpleDressup 是一个高性能的Unity换装系统，通过纹理图集合并和网格合并技术，将多个装备部件优化为单一渲染对象，大幅提升游戏性能。

## 快速开始

### 第一步：场景准备

1. **创建角色结构**
```
CharacterRoot (Empty GameObject)
├── Head (SkinnedMeshRenderer) - 角色头部
├── Body (SkinnedMeshRenderer) - 角色身体  
├── TargetRenderer (SkinnedMeshRenderer) - 合并结果输出
└── DressupParent (Empty GameObject) - 装备父节点
    ├── Hair_01 (SkinnedMeshRenderer)
    ├── Top_01 (SkinnedMeshRenderer)
    └── Bottom_01 (SkinnedMeshRenderer)
```

2. **添加SimpleDressupController组件**
```csharp
// 在CharacterRoot上添加SimpleDressupController组件
var controller = characterRoot.AddComponent<SimpleDressupController>();
```

### 第二步：配置Inspector参数

```
SimpleDressupController 组件设置：

基础配置：
- Atlas Size: 1024 (图集尺寸，建议1024或2048)  
- Auto Apply On Start: false (是否自动应用)
- Root Bone: Armature (角色根骨骼)

目标对象：
- Target Renderer: TargetRenderer (输出渲染器)
- Dressup Parent: DressupParent (装备父节点)

原始角色部件：
- Original Body Parts: [Head, Body] (原始角色部件数组)
```

### 第三步：基础代码使用

```csharp
public class MyDressupManager : MonoBehaviour 
{
    [SerializeField] private SimpleDressupController controller;
    [SerializeField] private GameObject[] hairOptions;
    [SerializeField] private GameObject[] topOptions;
    
    async void Start()
    {
        // 等待初始化完成
        while (!controller.IsInitialized) 
        {
            await UniTask.NextFrame();
        }
        
        // 自动检测原始角色部件
        controller.AutoDetectOriginalBodyParts();
        
        // 装备初始套装
        await EquipInitialOutfit();
    }
    
    async UniTask EquipInitialOutfit()
    {
        // 1. 创建装备GameObject
        var hair = Instantiate(hairOptions[0], controller.transform);
        var top = Instantiate(topOptions[0], controller.transform);
        
        // 2. 添加到换装系统
        controller.SetDressupSlotFromGameObject(hair, DressupSlotType.Hair);
        controller.SetDressupSlotFromGameObject(top, DressupSlotType.Top);
        
        // 3. 应用换装
        bool success = await controller.ApplyCurrentDressupAsync();
        
        if (success)
        {
            Debug.Log("换装成功！");
        }
    }
}
```

## 详细API说明

### 核心方法

#### 1. 设置装备
```csharp
// 方法1：从GameObject创建装备槽位
controller.SetDressupSlotFromGameObject(gameObject, slotType);

// 方法2：直接设置DressupSlot
var slot = ScriptableObject.CreateInstance<DressupSlot>();
slot.InitializeFromGameObject(gameObject);
slot.SlotType = DressupSlotType.Hair;
controller.SetDressupSlot(slot);
```

#### 2. 移除装备
```csharp
// 移除指定类型的装备
controller.RemoveDressupSlot(DressupSlotType.Hair);

// 清空所有装备
controller.ClearAllSlots();
```

#### 3. 应用换装
```csharp
// 异步应用换装（推荐）
bool success = await controller.ApplyCurrentDressupAsync();

// 订阅换装完成事件
controller.OnDressupComplete += (success) => {
    Debug.Log($"换装{(success ? "成功" : "失败")}");
};
```

#### 4. 自动收集
```csharp
// 自动检测原始角色部件
controller.AutoDetectOriginalBodyParts();

// 自动收集子对象中的装备
controller.CollectDressupSlotsFromChildren();
```

### 槽位类型

```csharp
[Flags]
public enum DressupSlotType
{
    None = 0,
    Face = 1 << 0,      // 脸部
    Hair = 1 << 1,      // 头发  
    Top = 1 << 2,       // 上衣
    Bottom = 1 << 3,    // 下衣
    Shoes = 1 << 4,     // 鞋子
    Gloves = 1 << 5,    // 手套
    Hat = 1 << 6,       // 帽子
    All = ~0            // 所有部位
}
```

## 完整使用示例

### 场景设置步骤

1. **创建空的GameObject作为角色根节点**
   - 命名为 "Character"

2. **添加原始角色部件**
   - 子对象 "Head" 包含 SkinnedMeshRenderer
   - 子对象 "Body" 包含 SkinnedMeshRenderer

3. **创建目标渲染器**
   - 子对象 "TargetRenderer" 包含空的 SkinnedMeshRenderer

4. **创建装备父节点**
   - 子对象 "DressupParent" 为空GameObject

5. **准备装备预制体**
   - Hair_01.prefab, Hair_02.prefab (包含SkinnedMeshRenderer)
   - Top_01.prefab, Top_02.prefab (包含SkinnedMeshRenderer)

### 脚本使用示例

参考 `SimpleDressupExample.cs` 中的完整实现：

1. **初始化阶段**
   - 等待系统初始化
   - 自动检测角色部件
   - 订阅事件

2. **装备阶段**
   - 实例化装备预制体
   - 添加到换装系统
   - 应用换装

3. **动态切换**
   - 销毁旧装备
   - 创建新装备
   - 重新应用换装

## 性能优化建议

### 1. 图集尺寸选择
```csharp
// 移动设备：512 或 1024
controller.AtlasSize = 1024;

// PC设备：1024 或 2048  
controller.AtlasSize = 2048;

// 高端设备：2048 或 4096
controller.AtlasSize = 4096;
```

### 2. 装备批量处理
```csharp
// ❌ 不推荐：逐个装备并应用
await EquipHair(0);
await controller.ApplyCurrentDressupAsync();
await EquipTop(0); 
await controller.ApplyCurrentDressupAsync();

// ✅ 推荐：批量装备后统一应用
await EquipHair(0);
await EquipTop(0);
await EquipBottom(0);
await controller.ApplyCurrentDressupAsync(); // 只应用一次
```

### 3. 内存管理
```csharp
// 及时销毁不用的装备GameObject
if (oldEquipment != null)
{
    DestroyImmediate(oldEquipment);
}

// 监听换装完成事件进行清理
controller.OnDressupComplete += (success) => {
    if (success)
    {
        CleanupTemporaryObjects();
    }
};
```

## 常见问题解决

### Q1: 换装后角色变形或消失
**原因**：骨骼映射问题
**解决**：确保所有装备使用相同的骨骼结构和命名

### Q2: 纹理显示异常
**原因**：图集生成失败或UV映射错误  
**解决**：检查材质设置，确保纹理可读性

### Q3: 性能问题
**原因**：频繁换装或图集过大
**解决**：使用批量换装，调整图集尺寸

### Q4: 编译错误
**原因**：缺少依赖
**解决**：确保导入 UniTask 和 XFramework.Utils

## 系统架构说明

```
SimpleDressupController (主控制器)
├── AtlasGenerator (图集生成器)
│   ├── TexturePacker (纹理装箱)
│   └── GL渲染合并
├── MeshCombiner (网格合并器)
│   ├── 顶点数据合并
│   ├── 骨骼权重重映射
│   └── 子网格管理
└── DressupSlot (装备槽位)
    ├── DressupMesh (网格数据)
    ├── DressupMaterial (材质数据)  
    └── TextureFragment (纹理片段)
```

## 总结

SimpleDressup 系统提供了完整的换装解决方案：
- ✅ 高性能：多对象合并为单一渲染
- ✅ 易用性：简单的API和自动化功能
- ✅ 灵活性：支持各种角色结构
- ✅ 扩展性：模块化设计便于定制

按照本指南操作，你就能快速集成并使用这个换装系统了！
