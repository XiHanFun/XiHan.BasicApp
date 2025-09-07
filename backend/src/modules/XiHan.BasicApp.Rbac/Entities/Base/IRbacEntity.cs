#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRbacEntity
// Guid:9c28152c-d6e9-4396-addb-b479254bad97
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Entities.Base;

/// <summary>
/// RBAC 基础实体接口
/// 定义了所有 RBAC 实体的基本结构
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IRbacEntity<TKey> where TKey : struct, IEquatable<TKey>
{
    /// <summary>
    /// 主键ID
    /// </summary>
    TKey BaseId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    DateTimeOffset? UpdatedTime { get; set; }
}

/// <summary>
/// 支持多租户的 RBAC 实体接口
/// 为支持多租户架构的实体提供租户ID字段
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IMultiTenantRbacEntity<TKey> : IRbacEntity<TKey> where TKey : struct, IEquatable<TKey>
{
    /// <summary>
    /// 租户ID
    /// 用于多租户数据隔离
    /// </summary>
    TKey? TenantId { get; set; }
}

/// <summary>
/// 与用户关联的 RBAC 实体接口
/// 为需要记录用户信息的实体提供用户ID字段
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IUserRelatedRbacEntity<TKey> : IRbacEntity<TKey> where TKey : struct, IEquatable<TKey>
{
    /// <summary>
    /// 用户ID
    /// 记录与此实体相关的用户
    /// </summary>
    TKey? UserId { get; set; }
}

/// <summary>
/// 可软删除的 RBAC 实体接口
/// 为支持软删除的实体提供删除标记字段
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public interface ISoftDeleteRbacEntity<TKey> : IRbacEntity<TKey> where TKey : struct, IEquatable<TKey>
{
    /// <summary>
    /// 是否已删除
    /// true: 已删除（软删除）, false: 未删除
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    DateTimeOffset? DeletedTime { get; set; }
}

/// <summary>
/// 支持审计的 RBAC 实体接口
/// 为需要记录操作人员的实体提供审计字段
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IAuditableRbacEntity<TKey> : IRbacEntity<TKey> where TKey : struct, IEquatable<TKey>
{
    /// <summary>
    /// 创建者ID
    /// </summary>
    TKey? CreatedBy { get; set; }

    /// <summary>
    /// 更新者ID
    /// </summary>
    TKey? UpdatedBy { get; set; }
}

/// <summary>
/// 完整功能的 RBAC 实体接口
/// 集成多租户、用户关联、软删除、审计等所有功能
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IFullFeatureRbacEntity<TKey> :
    IMultiTenantRbacEntity<TKey>,
    IUserRelatedRbacEntity<TKey>,
    ISoftDeleteRbacEntity<TKey>,
    IAuditableRbacEntity<TKey>
    where TKey : struct, IEquatable<TKey>
{
}

/// <summary>
/// 统一 ID 类型的基础 RBAC 实体接口
/// </summary>
public interface IRbacEntity : IRbacEntity<RbacIdType>
{
}

/// <summary>
/// 统一 ID 类型的多租户 RBAC 实体接口
/// </summary>
public interface IMultiTenantRbacEntity : IMultiTenantRbacEntity<RbacIdType>
{
}

/// <summary>
/// 统一 ID 类型的用户关联 RBAC 实体接口
/// </summary>
public interface IUserRelatedRbacEntity : IUserRelatedRbacEntity<RbacIdType>
{
}

/// <summary>
/// 统一 ID 类型的软删除 RBAC 实体接口
/// </summary>
public interface ISoftDeleteRbacEntity : ISoftDeleteRbacEntity<RbacIdType>
{
}

/// <summary>
/// 统一 ID 类型的审计 RBAC 实体接口
/// </summary>
public interface IAuditableRbacEntity : IAuditableRbacEntity<RbacIdType>
{
}

/// <summary>
/// 统一 ID 类型的完整功能 RBAC 实体接口
/// </summary>
public interface IFullFeatureRbacEntity : IFullFeatureRbacEntity<RbacIdType>
{
}
