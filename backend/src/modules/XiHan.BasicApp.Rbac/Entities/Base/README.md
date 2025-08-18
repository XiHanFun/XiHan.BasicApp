# RBAC 实体基类最佳实践指南

## 概述

本目录包含了 RBAC 模块实体的最佳实践实现，提供了灵活且统一的 ID 类型抽象化方案。

## 核心文件说明

### 1. RbacIdTypeConfiguration.cs

- **用途**: 全局 ID 类型配置
- **特点**: 通过 `global using` 实现统一的 ID 类型配置
- **推荐**: 这是最佳实践方案，简单易维护

### 2. IRbacEntity.cs

- **用途**: 定义各种功能的实体接口
- **包含**: 基础、多租户、用户关联、软删除、审计等接口
- **设计**: 支持泛型和统一 ID 类型两种方式

### 3. RbacEntityBase.cs

- **用途**: 提供不同功能的实体基类
- **层次**: 从基础到完整功能的多层次基类
- **灵活**: 支持按需选择所需功能

### 4. RbacEntityHelper.cs

- **用途**: 实体相关的工具方法和扩展方法
- **功能**: ID 处理、类型检查、便捷操作等
- **实用**: 提供丰富的辅助功能

### 5. RbacEntityUsageExamples.cs

- **用途**: 完整的使用示例
- **涵盖**: 各种场景的实体定义和使用方法
- **学习**: 最佳的学习和参考资料

## 快速开始

### 1. 配置 ID 类型

在 `RbacIdTypeConfiguration.cs` 中修改全局 ID 类型：

```csharp
// 使用 long (推荐)
global using RbacIdType = System.Int64;

// 使用 Guid (分布式场景推荐)
// global using RbacIdType = System.Guid;

// 使用 int (小项目)
// global using RbacIdType = System.Int32;

// 使用 string (自定义编码)
// global using RbacIdType = System.String;
```

### 2. 选择合适的基类

根据实体需要的功能选择对应的基类：

```csharp
// 基础实体（只需要 ID、创建时间、更新时间）
public class SimpleEntity : RbacEntityBase
{
    public string Name { get; set; } = string.Empty;
}

// 多租户实体
public class TenantEntity : MultiTenantRbacEntityBase
{
    public string Name { get; set; } = string.Empty;
}

// 完整功能实体（推荐用于业务实体）
public class BusinessEntity : FullFeatureRbacEntityBase
{
    public string BusinessNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}
```

### 3. 使用扩展方法

利用扩展方法简化实体操作：

```csharp
var entity = new BusinessEntity
{
    BusinessNumber = "BIZ001",
    Title = "示例业务"
};

// 设置创建信息
entity.SetTenant(tenantId)
      .SetUser(userId)
      .SetCreatedInfo(createdBy);

// 更新时设置更新信息
entity.SetUpdatedInfo(updatedBy);

// 软删除
entity.SetDeleted();
```

## 设计原则

### 1. 统一性原则

- 使用统一的 ID 类型配置
- 保持一致的命名规范
- 统一的实体结构

### 2. 灵活性原则

- 支持多种 ID 类型
- 提供不同功能级别的基类
- 可选的功能模块

### 3. 扩展性原则

- 接口驱动的设计
- 支持自定义扩展
- 向后兼容性

### 4. 易用性原则

- 提供丰富的扩展方法
- 完整的使用示例
- 详细的文档说明

## 功能矩阵

| 基类                        | 基础功能 | 多租户 | 用户关联 | 软删除 | 审计 | 适用场景     |
| --------------------------- | -------- | ------ | -------- | ------ | ---- | ------------ |
| `RbacEntityBase`            | ✅       | ❌     | ❌       | ❌     | ❌   | 简单实体     |
| `MultiTenantRbacEntityBase` | ✅       | ✅     | ❌       | ❌     | ❌   | 多租户场景   |
| `UserRelatedRbacEntityBase` | ✅       | ❌     | ✅       | ❌     | ❌   | 用户操作记录 |
| `FullFeatureRbacEntityBase` | ✅       | ✅     | ✅       | ✅     | ✅   | 完整业务实体 |

## 最佳实践建议

### 1. ID 类型选择

- **小型项目**: 使用 `int`
- **中大型项目**: 使用 `long`
- **分布式系统**: 使用 `Guid`
- **自定义编码**: 使用 `string`

### 2. 基类选择

- **简单配置表**: 使用 `RbacEntityBase`
- **业务数据表**: 使用 `FullFeatureRbacEntityBase`
- **日志记录表**: 使用 `UserRelatedRbacEntityBase`
- **多租户场景**: 确保使用包含 `MultiTenant` 的基类

### 3. 索引设计

```csharp
[SugarIndex("IX_EntityName_TenantId", "TenantId", OrderByType.Asc)]
[SugarIndex("IX_EntityName_UserId", "UserId", OrderByType.Asc)]
[SugarIndex("IX_EntityName_IsDeleted", "IsDeleted", OrderByType.Asc)]
[SugarIndex("IX_EntityName_CreatedTime", "CreatedTime", OrderByType.Desc)]
```

### 4. 实体操作

```csharp
// 创建时
entity.SetCreatedInfo(currentUserId);

// 更新时
entity.SetUpdatedInfo(currentUserId);

// 软删除
entity.SetDeleted();

// 多租户设置
entity.SetTenant(currentTenantId);
```

## 迁移指南

### 从现有实体迁移

1. **更新继承关系**:

   ```csharp
   // 原来
   public class SysUser : SugarEntityWithAudit<long>

   // 现在
   public class SysUser : FullFeatureRbacEntityBase
   ```

2. **更新 ID 相关字段**:

   ```csharp
   // 原来
   public long? TenantId { get; set; }

   // 现在
   public RbacIdType? TenantId { get; set; }
   ```

3. **添加索引**:
   ```csharp
   [SugarIndex("IX_SysUser_TenantId", "TenantId", OrderByType.Asc)]
   ```

## 注意事项

1. **数据库兼容性**: 切换 ID 类型需要考虑数据库迁移
2. **外键一致性**: 确保关联实体使用相同的 ID 类型
3. **性能考虑**: 不同 ID 类型的性能特征不同
4. **序列化**: 注意 JSON 序列化的兼容性

## 总结

这套 RBAC 实体基类设计遵循最佳实践，提供了：

- ✅ 统一的 ID 类型管理
- ✅ 灵活的功能选择
- ✅ 丰富的工具方法
- ✅ 完整的使用示例
- ✅ 详细的文档说明

推荐按照本指南的建议使用，可以大大提高开发效率和代码质量。
