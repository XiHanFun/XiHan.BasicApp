#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacEntityBase
// Guid:7a28152c-d6e9-4396-addb-b479254bad99
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.Framework.Data.SqlSugar.Entities;

namespace XiHan.BasicApp.Rbac.Entities.Base;

/// <summary>
/// RBAC 模块统一实体基类
/// 使用项目统一的ID类型，这是推荐的使用方式
/// </summary>
public abstract partial class RbacEntityBase : RbacEntityBase<RbacIdType>, IRbacEntity
{
}

/// <summary>
/// RBAC 模块泛型实体基类
/// 通过泛型参数 TKey 抽象化 ID 类型，适用于需要灵活ID类型的场景
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract partial class RbacEntityBase<TKey> : SugarEntityWithAudit<TKey>, IRbacEntity<TKey>
    where TKey : struct, IEquatable<TKey>
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public virtual TKey BaseId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public virtual DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public virtual DateTimeOffset? UpdatedTime { get; set; }
}

/// <summary>
/// 支持多租户的 RBAC 实体基类（使用统一ID类型）
/// </summary>
public abstract partial class MultiTenantRbacEntityBase : MultiTenantRbacEntityBase<RbacIdType>, IMultiTenantRbacEntity
{
}

/// <summary>
/// 支持多租户的 RBAC 实体基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract partial class MultiTenantRbacEntityBase<TKey> : RbacEntityBase<TKey>, IMultiTenantRbacEntity<TKey>
    where TKey : struct, IEquatable<TKey>
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
    public virtual TKey? TenantId { get; set; }
}

/// <summary>
/// 与用户关联的 RBAC 实体基类（使用统一ID类型）
/// </summary>
public abstract partial class UserRelatedRbacEntityBase : UserRelatedRbacEntityBase<RbacIdType>, IUserRelatedRbacEntity
{
}

/// <summary>
/// 与用户关联的 RBAC 实体基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract partial class UserRelatedRbacEntityBase<TKey> : RbacEntityBase<TKey>, IUserRelatedRbacEntity<TKey>
    where TKey : struct, IEquatable<TKey>
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = true)]
    public virtual TKey? UserId { get; set; }
}

/// <summary>
/// 完整功能的 RBAC 实体基类（使用统一ID类型）
/// 这是功能最全面的基类，推荐用于需要完整RBAC功能的实体
/// </summary>
public abstract partial class FullFeatureRbacEntityBase : FullFeatureRbacEntityBase<RbacIdType>, IFullFeatureRbacEntity
{
}

/// <summary>
/// 完整功能的 RBAC 实体基类
/// 集成多租户、用户关联等功能
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract partial class FullFeatureRbacEntityBase<TKey> : RbacEntityBase<TKey>, IFullFeatureRbacEntity<TKey>
    where TKey : struct, IEquatable<TKey>
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
    public virtual TKey? TenantId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = true)]
    public virtual TKey? UserId { get; set; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    [SugarColumn(ColumnDescription = "是否已删除")]
    public virtual bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 删除时间
    /// </summary>
    [SugarColumn(ColumnDescription = "删除时间", IsNullable = true)]
    public virtual DateTimeOffset? DeletedTime { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    [SugarColumn(ColumnDescription = "创建者ID", IsNullable = true)]
    public virtual TKey? CreatedBy { get; set; }

    /// <summary>
    /// 更新者ID
    /// </summary>
    [SugarColumn(ColumnDescription = "更新者ID", IsNullable = true)]
    public virtual TKey? UpdatedBy { get; set; }
}
