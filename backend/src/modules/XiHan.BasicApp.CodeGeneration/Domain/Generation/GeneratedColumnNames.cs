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
    /// 基类托管列（大小写不敏感，且忽略下划线）
    /// </summary>
    /// <remarks>
    /// 与 <c>SugarMultiTenantFullAuditedEntity</c> 及其基类声明的列一一对应。
    /// 库里的实际列名是 <c>Basic_Id</c> / <c>Created_Time</c> 这种带下划线的形式，
    /// 而此处按属性名书写，故比对前统一去下划线（见 <see cref="Normalize"/>）。
    /// </remarks>
    public static readonly IReadOnlySet<string> BaseColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "BasicId", "Id", "TenantId", "RowVersion", "IsDeleted",
        "CreatedTime", "CreatedId", "CreatedBy",
        "ModifiedTime", "ModifiedId", "ModifiedBy",
        "DeletedTime", "DeletedId", "DeletedBy"
    };

    /// <summary>
    /// 是否为基类托管的通用列
    /// </summary>
    /// <param name="columnName">数据库列名或实体属性名，两种写法均可</param>
    /// <returns>属基类托管返回 true</returns>
    public static bool IsBaseColumn(string columnName)
    {
        return !string.IsNullOrWhiteSpace(columnName) && BaseColumns.Contains(Normalize(columnName));
    }

    /// <summary>
    /// 归一化列名：去掉下划线，使 <c>Basic_Id</c> 与 <c>BasicId</c> 视为同一列
    /// </summary>
    private static string Normalize(string columnName)
    {
        return columnName.Replace("_", string.Empty, StringComparison.Ordinal);
    }
}
