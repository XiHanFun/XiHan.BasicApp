// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 基类托管的通用列名集合（主键/租户/审计/软删）
/// </summary>
/// <remarks>
/// 单一事实源：模板渲染据此判定 <c>col.IsBaseColumn</c> 只生成业务属性；
/// 推断引擎据此把通用列的列表/查询/新增/编辑开关默认关闭并标 <c>IsCommon</c>。
/// 两处共用，避免各维护一份导致漂移。
/// </remarks>
public static class GeneratedColumnNames
{
    /// <summary>
    /// 基类托管列（大小写不敏感）
    /// </summary>
    public static readonly IReadOnlySet<string> BaseColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "BasicId", "Id", "TenantId", "IsDeleted",
        "CreatedTime", "CreatedId", "CreatedBy",
        "ModifiedTime", "ModifiedId", "ModifiedBy",
        "DeletedTime", "DeletedId", "DeletedBy"
    };

    /// <summary>
    /// 是否为基类托管的通用列
    /// </summary>
    public static bool IsBaseColumn(string columnName)
    {
        return !string.IsNullOrWhiteSpace(columnName) && BaseColumns.Contains(columnName);
    }
}
