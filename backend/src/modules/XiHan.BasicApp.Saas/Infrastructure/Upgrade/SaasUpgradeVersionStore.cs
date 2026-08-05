// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Upgrade.Abstractions;
using XiHan.Framework.Upgrade.Models;

namespace XiHan.BasicApp.Saas.Infrastructure.Upgrade;

/// <summary>
/// 升级版本存储：把框架升级引擎的版本状态与迁移台账落到 <see cref="SysVersion"/> 与 <see cref="SysMigrationHistory"/>。
/// </summary>
/// <remarks>
/// 这两张实体的字段与框架的 <see cref="UpgradeVersionState"/>、<see cref="UpgradeMigrationHistory"/> 逐字段对应，
/// 本类型即它们一直缺席的实现槽。接上之后版本管理页展示的才是引擎写入的真实状态，而非人工台账。
/// <para>
/// 每个库各自持有自己的版本行与台账：平台库一行，库隔离租户各自独立库里各一行，
/// 由当前租户上下文经 <see cref="ISqlSugarClientResolver"/> 解析到对应连接。
/// </para>
/// </remarks>
public sealed class SaasUpgradeVersionStore : IUpgradeVersionStore
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasUpgradeVersionStore(ISqlSugarClientResolver clientResolver, ICurrentTenant currentTenant)
    {
        _clientResolver = clientResolver;
        _currentTenant = currentTenant;
    }

    private ISqlSugarClient Db => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 校验承载版本状态与台账的两张表是否就绪。
    /// </summary>
    /// <remarks>建表由框架的数据库初始化流程负责，此处只做前置校验，缺表即拒绝升级而不是静默跳过。</remarks>
    public Task EnsureTablesAsync(CancellationToken cancellationToken = default)
    {
        var db = Db;
        foreach (var tableName in new[]
        {
            db.EntityMaintenance.GetTableName<SysVersion>(),
            db.EntityMaintenance.GetTableName<SysMigrationHistory>()
        })
        {
            if (!db.DbMaintenance.IsAnyTable(tableName, false))
            {
                throw new InvalidOperationException($"升级所需的表 {tableName} 不存在，无法记录版本状态与执行台账。");
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 取当前库的版本行，没有则以「数据库版本 0.0.0」建一条，交由引擎把脚本逐版本推进上去。
    /// </summary>
    public async Task<UpgradeVersionState> GetOrCreateAsync(string currentAppVersion, string minSupportVersion, CancellationToken cancellationToken = default)
    {
        var existing = await Db.Queryable<SysVersion>()
            .OrderBy(version => version.CreatedTime, OrderByType.Desc)
            .FirstAsync(cancellationToken);

        if (existing is not null)
        {
            return ToState(existing);
        }

        var created = new SysVersion
        {
            AppVersion = currentAppVersion,
            DbVersion = "0.0.0",
            MinSupportVersion = minSupportVersion,
            IsUpgrading = false
        };
        created = await Db.Insertable(created).ExecuteReturnEntityAsync();

        return ToState(created);
    }

    /// <summary>
    /// 取最近一条台账，供状态查询展示。
    /// </summary>
    public async Task<UpgradeMigrationHistory?> GetLatestHistoryAsync(CancellationToken cancellationToken = default)
    {
        var latest = await Db.Queryable<SysMigrationHistory>()
            .OrderBy(history => history.ExecutedTime, OrderByType.Desc)
            .FirstAsync(cancellationToken);

        return latest is null ? null : ToHistory(latest);
    }

    /// <summary>
    /// 标记进入升级中，并记下执行节点与起始时刻。
    /// </summary>
    public async Task SetUpgradingAsync(UpgradeVersionState version, string nodeName, DateTimeOffset startTime, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        _ = await Db.Updateable<SysVersion>()
            .SetColumns(row => new SysVersion
            {
                IsUpgrading = true,
                UpgradeNode = nodeName,
                UpgradeStartTime = startTime
            })
            .Where(row => row.BasicId == version.Id)
            .ExecuteCommandAsync(cancellationToken);

        version.IsUpgrading = true;
        version.UpgradeNode = nodeName;
        version.UpgradeStartTime = startTime;
    }

    /// <summary>
    /// 升级成功：落回应用版本与数据库版本，清除升级中标记。
    /// </summary>
    public async Task SetUpgradeCompletedAsync(UpgradeVersionState version, string appVersion, string dbVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        _ = await Db.Updateable<SysVersion>()
            .SetColumns(row => new SysVersion
            {
                AppVersion = appVersion,
                DbVersion = dbVersion,
                IsUpgrading = false
            })
            .Where(row => row.BasicId == version.Id)
            .ExecuteCommandAsync(cancellationToken);

        version.AppVersion = appVersion;
        version.DbVersion = dbVersion;
        version.IsUpgrading = false;
    }

    /// <summary>
    /// 升级失败：清除升级中标记，保留已推进到的数据库版本，失败细节在台账里。
    /// </summary>
    public async Task SetUpgradeFailedAsync(UpgradeVersionState version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        _ = await Db.Updateable<SysVersion>()
            .SetColumns(row => new SysVersion { IsUpgrading = false })
            .Where(row => row.BasicId == version.Id)
            .ExecuteCommandAsync(cancellationToken);

        version.IsUpgrading = false;
    }

    /// <summary>
    /// 单个脚本成功后推进数据库版本指针。
    /// </summary>
    public async Task UpdateDbVersionAsync(UpgradeVersionState version, string dbVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        _ = await Db.Updateable<SysVersion>()
            .SetColumns(row => new SysVersion { DbVersion = dbVersion })
            .Where(row => row.BasicId == version.Id)
            .ExecuteCommandAsync(cancellationToken);

        version.DbVersion = dbVersion;
    }

    /// <summary>
    /// 追加一条执行台账。
    /// </summary>
    public async Task AddMigrationHistoryAsync(UpgradeMigrationHistory history, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);

        var entity = new SysMigrationHistory
        {
            Version = history.Version,
            ScriptName = history.ScriptName,
            ExecutedTime = history.ExecutedTime,
            Success = history.Success,
            NodeName = history.NodeName,
            ErrorMessage = history.ErrorMessage?.Length > 1024 ? history.ErrorMessage[..1024] : history.ErrorMessage
        };

        _ = await Db.Insertable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 判断某脚本是否已成功执行过，供引擎在版本指针之外再做一次逐脚本去重。
    /// </summary>
    public async Task<bool> HasMigrationHistoryAsync(string version, string scriptName, CancellationToken cancellationToken = default)
    {
        return await Db.Queryable<SysMigrationHistory>()
            .Where(history => history.Version == version && history.ScriptName == scriptName && history.Success)
            .AnyAsync(cancellationToken);
    }

    private UpgradeVersionState ToState(SysVersion entity)
    {
        return new UpgradeVersionState
        {
            Id = entity.BasicId,
            TenantId = _currentTenant.Id,
            AppVersion = entity.AppVersion,
            DbVersion = string.IsNullOrWhiteSpace(entity.DbVersion) ? "0.0.0" : entity.DbVersion,
            MinSupportVersion = entity.MinSupportVersion,
            IsUpgrading = entity.IsUpgrading,
            UpgradeNode = entity.UpgradeNode,
            UpgradeStartTime = entity.UpgradeStartTime
        };
    }

    private static UpgradeMigrationHistory ToHistory(SysMigrationHistory entity)
    {
        return new UpgradeMigrationHistory
        {
            TenantId = entity.TenantId,
            Version = entity.Version,
            ScriptName = entity.ScriptName,
            ExecutedTime = entity.ExecutedTime,
            Success = entity.Success,
            NodeName = entity.NodeName,
            ErrorMessage = entity.ErrorMessage
        };
    }
}
