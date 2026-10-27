// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 应用实体基类：UUID v7 主键 + 并发戳。用于关联表与无审计需求的表。
/// 主键由 <c>IIdGenerator&lt;Guid&gt;</c> 在创建时分配，并发戳由框架在每次写入时更新。
/// </summary>
public abstract class AppEntity : IEntity<Guid>, IHasConcurrencyStamp
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
