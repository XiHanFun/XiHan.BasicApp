#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenEntity
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567002
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Data.SqlSugar.Entities;

namespace XiHan.BasicApp.CodeGeneration.Entities.Base;

/// <summary>
/// CodeGen 实体基类（泛型主键）
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class CodeGenEntity<TKey> : SugarEntity<TKey>
    where TKey : IEquatable<TKey>
{
}

/// <summary>
/// CodeGen 实体基类（自增主键）
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class CodeGenEntityWithIdentity<TKey> : SugarEntityWithIdentity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    protected CodeGenEntityWithIdentity() : base()
    {
    }
}

/// <summary>
/// CodeGen 创建审计实体基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class CodeGenCreationEntity<TKey> : SugarCreationEntity<TKey>
    where TKey : IEquatable<TKey>
{
}

/// <summary>
/// CodeGen 修改审计实体基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class CodeGenModificationEntity<TKey> : SugarModificationEntity<TKey>
    where TKey : IEquatable<TKey>
{
}

/// <summary>
/// CodeGen 删除审计实体基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class CodeGenDeletionEntity<TKey> : SugarDeletionEntity<TKey>
    where TKey : IEquatable<TKey>
{
}

/// <summary>
/// CodeGen 完整审计实体基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class CodeGenFullAuditedEntity<TKey> : SugarFullAuditedEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    protected CodeGenFullAuditedEntity() : base()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="basicId">主键</param>
    protected CodeGenFullAuditedEntity(TKey basicId) : base(basicId)
    {
    }
}

