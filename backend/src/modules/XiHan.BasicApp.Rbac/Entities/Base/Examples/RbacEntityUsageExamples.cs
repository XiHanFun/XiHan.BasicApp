#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacEntityUsageExamples
// Guid:ec28152c-d6e9-4396-addb-b479254bad92
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities.Base.Examples;

#region 基础实体示例

/// <summary>
/// 示例：基础实体
/// 适用于简单的实体，只需要基本的 ID、创建时间、更新时间
/// </summary>
[SugarTable("example_basic", "基础实体示例表")]
public class BasicExample : RbacEntityBase
{
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnDescription = "名称", Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(ColumnDescription = "描述", Length = 500, IsNullable = true)]
    public string? Description { get; set; }
}

#endregion

#region 多租户实体示例

/// <summary>
/// 示例：多租户实体
/// 适用于需要按租户隔离数据的场景
/// </summary>
[SugarTable("example_multi_tenant", "多租户实体示例表")]
[SugarIndex("IX_MultiTenantExample_TenantId", "TenantId", OrderByType.Asc)]
public class MultiTenantExample : MultiTenantRbacEntityBase
{
    /// <summary>
    /// 产品名称
    /// </summary>
    [SugarColumn(ColumnDescription = "产品名称", Length = 100, IsNullable = false)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 价格
    /// </summary>
    [SugarColumn(ColumnDescription = "价格", DecimalDigits = 2)]
    public decimal Price { get; set; }
}

#endregion

#region 用户关联实体示例

/// <summary>
/// 示例：与用户关联的实体
/// 适用于需要记录用户操作的场景
/// </summary>
[SugarTable("example_user_related", "用户关联实体示例表")]
[SugarIndex("IX_UserRelatedExample_UserId", "UserId", OrderByType.Asc)]
public class UserRelatedExample : UserRelatedRbacEntityBase
{
    /// <summary>
    /// 操作类型
    /// </summary>
    [SugarColumn(ColumnDescription = "操作类型", Length = 50, IsNullable = false)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作内容
    /// </summary>
    [SugarColumn(ColumnDescription = "操作内容", ColumnDataType = "text", IsNullable = true)]
    public string? OperationContent { get; set; }
}

#endregion

#region 完整功能实体示例

/// <summary>
/// 业务状态枚举
/// </summary>
public enum BusinessStatus
{
    /// <summary>
    /// 草稿
    /// </summary>
    Draft = 0,

    /// <summary>
    /// 待审核
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 已通过
    /// </summary>
    Approved = 2,

    /// <summary>
    /// 已拒绝
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// 已发布
    /// </summary>
    Published = 4
}

/// <summary>
/// 示例：完整功能实体
/// 适用于需要完整 RBAC 功能的业务实体
/// 包含多租户、用户关联、软删除、审计等所有功能
/// </summary>
[SugarTable("example_full_feature", "完整功能实体示例表")]
[SugarIndex("IX_FullFeatureExample_TenantId", "TenantId", OrderByType.Asc)]
[SugarIndex("IX_FullFeatureExample_UserId", "UserId", OrderByType.Asc)]
[SugarIndex("IX_FullFeatureExample_IsDeleted", "IsDeleted", OrderByType.Asc)]
public class FullFeatureExample : FullFeatureRbacEntityBase
{
    /// <summary>
    /// 业务编号
    /// </summary>
    [SugarColumn(ColumnDescription = "业务编号", Length = 50, IsNullable = false)]
    public string BusinessNumber { get; set; } = string.Empty;

    /// <summary>
    /// 标题
    /// </summary>
    [SugarColumn(ColumnDescription = "标题", Length = 200, IsNullable = false)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    [SugarColumn(ColumnDescription = "内容", ColumnDataType = "text", IsNullable = true)]
    public string? Content { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public BusinessStatus Status { get; set; } = BusinessStatus.Draft;

    /// <summary>
    /// 优先级
    /// </summary>
    [SugarColumn(ColumnDescription = "优先级")]
    public int Priority { get; set; } = 0;
}

#endregion

#region 自定义 ID 类型示例

/// <summary>
/// 示例：使用自定义 ID 类型的实体
/// 当需要使用特定 ID 类型时（如 Guid），可以使用泛型基类
/// </summary>
[SugarTable("example_guid_id", "GUID ID 实体示例表")]
public class GuidIdExample : RbacEntityBase<Guid>
{
    /// <summary>
    /// 外部系统ID
    /// </summary>
    [SugarColumn(ColumnDescription = "外部系统ID", Length = 100, IsNullable = true)]
    public string? ExternalSystemId { get; set; }

    /// <summary>
    /// 同步状态
    /// </summary>
    [SugarColumn(ColumnDescription = "同步状态")]
    public bool IsSynced { get; set; } = false;
}

#endregion

#region 使用方法示例

/// <summary>
/// 实体使用方法示例类
/// 展示如何在代码中正确使用这些实体
/// </summary>
public static class EntityUsageExamples
{
    /// <summary>
    /// 创建基础实体示例
    /// </summary>
    public static BasicExample CreateBasicExample()
    {
        var entity = new BasicExample
        {
            Name = "示例名称",
            Description = "这是一个基础实体示例"
        };

        // 使用扩展方法设置创建信息
        entity.SetCreatedInfo();

        return entity;
    }

    /// <summary>
    /// 创建多租户实体示例
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    public static MultiTenantExample CreateMultiTenantExample(RbacIdType tenantId)
    {
        var entity = new MultiTenantExample
        {
            ProductName = "示例产品",
            Price = 99.99m
        };

        // 设置租户和创建信息
        entity.SetTenant(tenantId)
              .SetCreatedInfo();

        return entity;
    }

    /// <summary>
    /// 创建用户关联实体示例
    /// </summary>
    /// <param name="userId">用户ID</param>
    public static UserRelatedExample CreateUserRelatedExample(RbacIdType userId)
    {
        var entity = new UserRelatedExample
        {
            OperationType = "LOGIN",
            OperationContent = "用户登录操作"
        };

        // 设置用户和创建信息
        entity.SetUser(userId)
              .SetCreatedInfo();

        return entity;
    }

    /// <summary>
    /// 创建完整功能实体示例
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="createdBy">创建者ID</param>
    public static FullFeatureExample CreateFullFeatureExample(
        RbacIdType tenantId,
        RbacIdType userId,
        RbacIdType createdBy)
    {
        var entity = new FullFeatureExample
        {
            BusinessNumber = $"BIZ{DateTime.Now:yyyyMMddHHmmss}",
            Title = "示例业务",
            Content = "这是一个完整功能的实体示例",
            Status = BusinessStatus.Draft,
            Priority = 1
        };

        // 设置完整信息
        entity.SetTenant(tenantId)
              .SetUser(userId)
              .SetCreatedInfo(createdBy);

        return entity;
    }

    /// <summary>
    /// 更新实体示例
    /// </summary>
    /// <param name="entity">要更新的实体</param>
    /// <param name="updatedBy">更新者ID</param>
    public static void UpdateExample(FullFeatureExample entity, RbacIdType updatedBy)
    {
        entity.Title = "更新后的标题";
        entity.Status = BusinessStatus.Pending;

        // 设置更新信息
        entity.SetUpdatedInfo(updatedBy);
    }

    /// <summary>
    /// 软删除实体示例
    /// </summary>
    /// <param name="entity">要删除的实体</param>
    public static void SoftDeleteExample(FullFeatureExample entity)
    {
        // 使用扩展方法设置软删除
        entity.SetDeleted();
    }

    /// <summary>
    /// 恢复软删除实体示例
    /// </summary>
    /// <param name="entity">要恢复的实体</param>
    public static void RestoreExample(FullFeatureExample entity)
    {
        // 使用扩展方法恢复软删除
        entity.SetUndeleted();
    }

    /// <summary>
    /// 使用帮助类的示例
    /// </summary>
    public static void HelperUsageExample()
    {
        // 获取当前 ID 类型信息
        var currentIdType = RbacEntityHelper.GetCurrentIdType();
        var currentIdTypeName = RbacEntityHelper.GetCurrentIdTypeName();

        Console.WriteLine($"当前 ID 类型: {currentIdTypeName}");

        // 创建新的 ID
        var newId = RbacEntityHelper.CreateNewId();
        Console.WriteLine($"新创建的 ID: {newId}");

        // 验证 ID 是否有效
        var isValid = RbacEntityHelper.IsValidId(newId);
        Console.WriteLine($"ID 是否有效: {isValid}");

        // 检查实体类型的功能支持
        var entityType = typeof(FullFeatureExample);
        Console.WriteLine($"是否支持多租户: {RbacEntityHelper.IsMultiTenant(entityType)}");
        Console.WriteLine($"是否与用户关联: {RbacEntityHelper.IsUserRelated(entityType)}");
        Console.WriteLine($"是否支持软删除: {RbacEntityHelper.IsSoftDeleteEnabled(entityType)}");
        Console.WriteLine($"是否支持审计: {RbacEntityHelper.IsAuditable(entityType)}");
    }
}

#endregion
