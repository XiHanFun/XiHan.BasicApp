// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Runtime.Schema;

namespace XiHan.BasicApp.Maintenance.Operations.Versions;

/// <summary>
/// 指定一个库的请求
/// </summary>
/// <param name="Id">哪个库：平台库为 <c>platform</c>，独立库为租户 ID</param>
public sealed record VersionIdRequest([Required, StringLength(64, MinimumLength = 1)] string Id);

/// <summary>
/// 手动释放升级锁的请求
/// </summary>
/// <param name="Id">哪个库：平台库为 <c>platform</c>，独立库为租户 ID</param>
/// <param name="Node">持锁的节点（版本台账上显示的）：锁还在它手里才释放</param>
public sealed record ReleaseVersionLockRequest([Required, StringLength(64, MinimumLength = 1)] string Id, [Required, StringLength(128, MinimumLength = 1)] string Node);

/// <summary>
/// 库的结构状态
/// </summary>
public enum SchemaVersionStatus
{
    /// <summary>
    /// 在应用的结构版本
    /// </summary>
    Current = 0,

    /// <summary>
    /// 结构版本与应用的不一致，还没有升级
    /// </summary>
    Behind = 1,

    /// <summary>
    /// 有节点持着没过期的升级锁
    /// </summary>
    Upgrading = 2,

    /// <summary>
    /// 最近一次升级失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 读不到这个库的台账
    /// </summary>
    Unreachable = 4,
}

/// <summary>
/// 一个库的版本台账
/// </summary>
/// <param name="Id">台账行 ID</param>
/// <param name="Database">哪个库：平台库为 <c>platform</c></param>
/// <param name="Provider">库类型；读不到台账时为空</param>
/// <param name="Status">结构状态</param>
/// <param name="AppVersion">最近一次升级这个库的应用版本</param>
/// <param name="DbVersion">库的结构版本；新库还没升级为 <see langword="null"/></param>
/// <param name="TargetVersion">应用带的结构版本</param>
/// <param name="IsUpgrading">是否持着升级锁（过期的锁也算，可以手动释放）</param>
/// <param name="UpgradeNode">持有升级锁、或最近一次持锁的节点</param>
/// <param name="UpgradeStartedAt">这一轮升级的开始时间</param>
/// <param name="UpgradedAt">最近一次升级完成的时间</param>
/// <param name="LastError">最近一次升级失败的原因；成功后清空</param>
/// <param name="LastFailedAt">最近一次升级失败的时间</param>
/// <param name="Error">读不到这个库的台账时的原因（其余列为空）</param>
public sealed record SchemaVersionResponse(
    Guid Id,
    string Database,
    string? Provider,
    SchemaVersionStatus Status,
    string AppVersion,
    string? DbVersion,
    string? TargetVersion,
    bool IsUpgrading,
    string? UpgradeNode,
    DateTimeOffset? UpgradeStartedAt,
    DateTimeOffset? UpgradedAt,
    string? LastError,
    DateTimeOffset? LastFailedAt,
    string? Error = null)
{
    /// <summary>
    /// 平台库的库名
    /// </summary>
    public const string Platform = SchemaUpgrader.PlatformDatabase;

    /// <summary>
    /// 从台账行投影
    /// </summary>
    /// <param name="row">台账行</param>
    /// <param name="database">库名</param>
    /// <param name="provider">库类型</param>
    /// <param name="target">应用带的结构版本</param>
    /// <param name="lockExpired">锁过了这个时刻开始的即视为已失效</param>
    public static SchemaVersionResponse From(SysVersion row, string database, string provider, string target, DateTimeOffset lockExpired)
    {
        ArgumentNullException.ThrowIfNull(row);
        var status = row.IsUpgrading && row.UpgradeStartedAt > lockExpired ? SchemaVersionStatus.Upgrading
            : row.LastError is not null ? SchemaVersionStatus.Failed
            : string.Equals(row.DbVersion, target, StringComparison.Ordinal) ? SchemaVersionStatus.Current
            : SchemaVersionStatus.Behind;
        return new SchemaVersionResponse(row.Id, database, provider, status, row.AppVersion, row.DbVersion, target, row.IsUpgrading, row.UpgradeNode, row.UpgradeStartedAt, row.UpgradedAt, row.LastError, row.LastFailedAt);
    }

    /// <summary>
    /// 读不到台账的库
    /// </summary>
    public static SchemaVersionResponse Unreadable(string database, string error)
        => new(SysVersion.RowId, database, null, SchemaVersionStatus.Unreachable, string.Empty, null, null, false, null, null, null, null, null, error);
}

/// <summary>
/// 一个库升到当前应用版本还要做的结构变更
/// </summary>
/// <param name="Database">库名</param>
/// <param name="Provider">库类型</param>
/// <param name="From">台账上的结构版本；新库为 <see langword="null"/></param>
/// <param name="To">应用带的结构版本</param>
/// <param name="SyncDue">这次升级会不会按实体建表补列：新库、有脚本要执行或应用版本变了才做</param>
/// <param name="Scripts">要执行的脚本，按执行顺序</param>
/// <param name="Tables">要建的表与要加的列</param>
/// <param name="Indexes">要建的索引</param>
public sealed record SchemaPlanResponse(
    string Database,
    string Provider,
    string? From,
    string To,
    bool SyncDue,
    IReadOnlyList<SchemaPlanScriptResponse> Scripts,
    IReadOnlyList<SchemaPlanTableResponse> Tables,
    IReadOnlyList<SchemaPlanIndexResponse> Indexes)
{
    /// <summary>
    /// 从预览投影
    /// </summary>
    public static SchemaPlanResponse Of(SchemaPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return new SchemaPlanResponse(
            plan.Database,
            plan.Provider,
            plan.From,
            plan.To,
            plan.SyncDue,
            [.. plan.Scripts.Select(static s => new SchemaPlanScriptResponse(s.Version, s.Script, s.Sql))],
            [.. plan.Tables.Select(static t => new SchemaPlanTableResponse(t.Table, t.IsNew, [.. t.Columns.Select(static c => new SchemaPlanColumnResponse(c.Column, c.Type, c.IsNullable))]))],
            [.. plan.Indexes.Select(static i => new SchemaPlanIndexResponse(i.Table, i.Index))]);
    }
}

/// <summary>
/// 一个要执行的升级脚本
/// </summary>
/// <param name="Version">所属的版本</param>
/// <param name="Script">脚本名（库类型目录起的相对路径）</param>
/// <param name="Sql">脚本原文</param>
public sealed record SchemaPlanScriptResponse(string Version, string Script, string Sql);

/// <summary>
/// 一张要建的表，或一张已有的表上要加的列
/// </summary>
/// <param name="Table">表名</param>
/// <param name="IsNew">是否新表（是即列为全部列）</param>
/// <param name="Columns">要建或要加的列</param>
public sealed record SchemaPlanTableResponse(string Table, bool IsNew, IReadOnlyList<SchemaPlanColumnResponse> Columns);

/// <summary>
/// 一个要建或要加的列
/// </summary>
/// <param name="Column">列名</param>
/// <param name="Type">列类型（按这个库的类型名，带长度）</param>
/// <param name="IsNullable">是否可空</param>
public sealed record SchemaPlanColumnResponse(string Column, string Type, bool IsNullable);

/// <summary>
/// 一个要建的索引
/// </summary>
/// <param name="Table">所在的表</param>
/// <param name="Index">索引名</param>
public sealed record SchemaPlanIndexResponse(string Table, string Index);

/// <summary>
/// 重试升级一个租户独立库的结果
/// </summary>
/// <param name="Database">库名（租户 ID）</param>
/// <param name="RunId">这次升级的批次号（升级记录里按它筛）</param>
/// <param name="From">升级前的结构版本</param>
/// <param name="To">升级后的结构版本</param>
/// <param name="Applied">执行了的步骤</param>
public sealed record SchemaUpgradeResponse(string Database, Guid RunId, string? From, string? To, IReadOnlyList<string> Applied);
