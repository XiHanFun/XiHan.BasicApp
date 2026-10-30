// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Runtime.Schema;

namespace XiHan.BasicApp.Maintenance.Operations.Migrations;

/// <summary>
/// 按 ID 取一条执行记录的请求。
/// </summary>
/// <param name="Id">记录 ID。</param>
public sealed record MigrationIdRequest(Guid Id);

/// <summary>
/// 一个升级脚本的执行记录。
/// </summary>
/// <param name="Id">记录 ID。</param>
/// <param name="Version">脚本所在的版本目录。</param>
/// <param name="ScriptName">脚本文件名。</param>
/// <param name="ExecutedAt">开始执行的时间。</param>
/// <param name="DurationMs">耗时（毫秒）。</param>
/// <param name="Success">是否成功。</param>
/// <param name="NodeName">执行的节点。</param>
/// <param name="ErrorMessage">失败的原因。</param>
public sealed record MigrationResponse(Guid Id, string Version, string ScriptName, DateTimeOffset ExecutedAt, long DurationMs, bool Success, string? NodeName, string? ErrorMessage)
{
    /// <summary>
    /// 从执行记录投影。
    /// </summary>
    public static MigrationResponse From(SysMigrationHistory row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return new MigrationResponse(row.Id, row.Version, row.ScriptName, row.ExecutedAt, row.DurationMs, row.Success, row.NodeName, row.ErrorMessage);
    }
}
