// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 全审计实体基类（默认选择）
/// </summary>
/// <remarks>
/// <see cref="AppRecord"/> + 修改时间 / 修改人 + 软删除 / 删除人
/// 时间与删除标记由框架维护，操作人由 <c>Runtime</c> 在写库时填充；唯一约束用 <see cref="ActiveUniqueIndexAttribute"/>（只约束未删除的行）
/// </remarks>
public abstract class AppAuditedEntity : AppRecord, IHasModificationTime, IHasModifier, ISoftDeletable, IHasDeleter
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "modified_at", IsNullable = true, ColumnDescription = "最后修改时间")]
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnName = "modified_by", Length = 512, IsNullable = true, ColumnDescription = "最后修改人（主体 ID）")]
    public string? ModifiedBy { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnName = "is_deleted", IsNullable = false, ColumnDescription = "是否已删除")]
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnName = "deleted_at", IsNullable = true, ColumnDescription = "删除时间")]
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnName = "deleted_by", Length = 512, IsNullable = true, ColumnDescription = "删除人（主体 ID）")]
    public string? DeletedBy { get; set; }
}
