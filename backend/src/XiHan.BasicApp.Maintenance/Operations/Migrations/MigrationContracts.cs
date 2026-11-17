// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Runtime.Schema;

namespace XiHan.BasicApp.Maintenance.Operations.Migrations;

/// <summary>
/// 按 ID 取一条执行记录的请求
/// </summary>
/// <param name="Id">记录 ID</param>
/// <param name="Database">记录所在的库：不填为平台库，独立库为租户 ID</param>
public sealed record MigrationIdRequest(Guid Id, [StringLength(64, MinimumLength = 1)] string? Database = null);

/// <summary>
/// 一步结构升级的执行记录
/// </summary>
/// <param name="Id">记录 ID</param>
/// <param name="Database">记录所在的库：平台库为 <c>platform</c>，独立库为租户 ID</param>
/// <param name="RunId">所属的一次升级（同一次升级里各库的步骤相同；更早的记录为空）</param>
/// <param name="Version">这一步所属的版本</param>
/// <param name="ScriptName">脚本名，按实体建表补列为 <c>orm-sync</c></param>
/// <param name="ExecutedAt">开始执行的时间</param>
/// <param name="DurationMs">耗时（毫秒）</param>
/// <param name="Success">是否成功</param>
/// <param name="NodeName">执行的节点</param>
/// <param name="ErrorMessage">失败的原因</param>
/// <param name="HasSql">是否记下了执行的 SQL（取单条时带原文）</param>
public sealed record MigrationResponse(Guid Id, string Database, Guid? RunId, string Version, string ScriptName, DateTimeOffset ExecutedAt, long DurationMs, bool Success, string? NodeName, string? ErrorMessage, bool HasSql)
{
    /// <summary>
    /// 从执行记录投影
    /// </summary>
    public static MigrationResponse From(SysMigrationHistory row, string database)
    {
        ArgumentNullException.ThrowIfNull(row);
        return new MigrationResponse(row.Id, database, row.RunId, row.Version, row.ScriptName, row.ExecutedAt, row.DurationMs, row.Success, row.NodeName, row.ErrorMessage, !string.IsNullOrEmpty(row.SqlText));
    }
}

/// <summary>
/// 一步结构升级的执行记录与执行的 SQL
/// </summary>
/// <param name="Migration">执行记录</param>
/// <param name="SqlText">执行的 SQL：脚本原文，或按实体建表补列实际发出的语句；没有记下为空</param>
public sealed record MigrationDetailResponse(MigrationResponse Migration, string? SqlText)
{
    /// <summary>
    /// 从执行记录投影
    /// </summary>
    public static MigrationDetailResponse From(SysMigrationHistory row, string database)
    {
        ArgumentNullException.ThrowIfNull(row);
        return new MigrationDetailResponse(MigrationResponse.From(row, database), row.SqlText);
    }
}
