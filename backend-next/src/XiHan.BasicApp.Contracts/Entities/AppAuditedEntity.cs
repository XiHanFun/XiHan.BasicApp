// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 全审计实体基类（默认选择）：<see cref="AppRecord"/> + 修改时间 / 修改人 + 软删除 / 删除人。
/// 时间与删除标记由框架维护，操作人由 <c>Runtime</c> 在写库时填充；软删除实体的唯一索引末列须含 <c>is_deleted</c>。
/// </summary>
public abstract class AppAuditedEntity : AppRecord, IHasModificationTime, IHasModifier, ISoftDeletable, IHasDeleter
{
    /// <inheritdoc />
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <inheritdoc />
    public string? ModifiedBy { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    public string? DeletedBy { get; set; }
}
