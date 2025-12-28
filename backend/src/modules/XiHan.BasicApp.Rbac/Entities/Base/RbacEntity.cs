#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacEntity
// Guid:892d867f-e909-4e3c-80c9-81c24b8e07dc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/10 6:33:01
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Data.SqlSugar.Entities;

namespace XiHan.BasicApp.Rbac.Entities.Base;

/// <summary>
/// Rbac 实体基类（泛型主键）
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class RbacEntity<TKey> : SugarEntity<TKey>
    where TKey : IEquatable<TKey>
{
}

/// <summary>
/// Rbac 实体基类（自增主键）
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class RbacEntityWithIdentity<TKey> : SugarEntityWithIdentity<TKey>
    where TKey : IEquatable<TKey>
{
}

/// <summary>
/// Rbac 创建审计实体基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class RbacCreationEntity<TKey> : SugarCreationEntity<TKey>
    where TKey : IEquatable<TKey>
{
}

/// <summary>
/// Rbac 修改审计实体基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class RbacModificationEntity<TKey> : SugarModificationEntity<TKey>
    where TKey : IEquatable<TKey>
{
}

/// <summary>
/// Rbac 删除审计实体基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class RbacDeletionEntity<TKey> : SugarDeletionEntity<TKey>
    where TKey : IEquatable<TKey>
{
}

/// <summary>
/// Rbac 完整审计实体基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class RbacFullAuditedEntity<TKey> : SugarFullAuditedEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    protected RbacFullAuditedEntity() : base()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="basicId">主键</param>
    protected RbacFullAuditedEntity(TKey basicId) : base(basicId)
    {
    }
}
