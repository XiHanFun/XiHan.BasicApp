// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Runtime.Schema;

namespace XiHan.BasicApp.Maintenance.Operations.Versions;

/// <summary>
/// 按 ID 取一条版本台账的请求。
/// </summary>
/// <param name="Id">台账行 ID。</param>
public sealed record VersionIdRequest(Guid Id);

/// <summary>
/// 一个库的版本台账。
/// </summary>
/// <param name="Id">台账行 ID。</param>
/// <param name="Database">哪个库：平台库为 <c>platform</c>。</param>
/// <param name="AppVersion">最近一次升级这个库的应用版本。</param>
/// <param name="DbVersion">库的结构版本；还没执行过基线为 <see langword="null"/>。</param>
/// <param name="IsUpgrading">是否正在升级。</param>
/// <param name="UpgradeNode">持有升级锁的节点。</param>
/// <param name="UpgradeStartedAt">这一轮升级的开始时间。</param>
/// <param name="UpgradedAt">最近一次升级完成的时间。</param>
public sealed record SchemaVersionResponse(Guid Id, string Database, string AppVersion, string? DbVersion, bool IsUpgrading, string? UpgradeNode, DateTimeOffset? UpgradeStartedAt, DateTimeOffset? UpgradedAt)
{
    /// <summary>
    /// 从台账行投影。
    /// </summary>
    public static SchemaVersionResponse From(SysVersion row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return new SchemaVersionResponse(row.Id, "platform", row.AppVersion, row.DbVersion, row.IsUpgrading, row.UpgradeNode, row.UpgradeStartedAt, row.UpgradedAt);
    }
}
