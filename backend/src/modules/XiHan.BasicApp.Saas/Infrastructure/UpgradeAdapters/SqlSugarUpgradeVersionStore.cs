#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SqlSugarUpgradeVersionStore
// Guid:fe2f6b54-1af4-4c21-9b4a-2f420c1e1c8a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 16:49:30
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Options;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Upgrade.Abstractions;
using XiHan.Framework.Upgrade.Models;
using XiHan.Framework.Upgrade.Options;

namespace XiHan.BasicApp.Saas.Infrastructure.UpgradeAdapters;

/// <summary>
/// 基于 SqlSugar 的升级版本存储
/// </summary>
public class SqlSugarUpgradeVersionStore : IUpgradeVersionStore
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly XiHanUpgradeOptions _options;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientResolver"></param>
    /// <param name="options">升级选项</param>
    /// <param name="currentTenant">当前租户</param>
    public SqlSugarUpgradeVersionStore(
        ISqlSugarClientResolver clientResolver,
        IOptions<XiHanUpgradeOptions> options,
        ICurrentTenant currentTenant)
    {
        _clientResolver = clientResolver;
        _options = options.Value;
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 确保升级相关表存在
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task EnsureTablesAsync(CancellationToken cancellationToken = default)
    {
        var db = GetDbClient();
        await Task.Run(() =>
        {
            db.CodeFirst.InitTables<SysVersion, SysMigrationHistory, SysUpgradeLock>();
        }, cancellationToken);
    }

    /// <summary>
    /// 获取或创建升级版本状态
    /// </summary>
    /// <param name="currentAppVersion">当前应用版本</param>
    /// <param name="minSupportVersion">最小支持版本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<UpgradeVersionState> GetOrCreateAsync(string currentAppVersion, string minSupportVersion, CancellationToken cancellationToken = default)
    {
        await EnsureTablesAsync(cancellationToken);
        var db = GetDbClient();
        var tenantId = GetTenantId();

        var query = db.Queryable<SysVersion>();
        query = tenantId.HasValue
            ? query.Where(v => v.TenantId == tenantId)
            : query.Where(v => v.TenantId == 0);

        var version = await query.FirstAsync(cancellationToken);
        if (version == null)
        {
            var entity = new SysVersion
            {
                TenantId = tenantId ?? 0,
                AppVersion = currentAppVersion,
                DbVersion = "0.0.0",
                MinSupportVersion = minSupportVersion,
                IsUpgrading = false
            };

            var id = await db.Insertable(entity).ExecuteReturnSnowflakeIdAsync(cancellationToken);
            return MapVersion(entity);
        }

        if (!string.Equals(version.MinSupportVersion, minSupportVersion, StringComparison.OrdinalIgnoreCase))
        {
            version.MinSupportVersion = minSupportVersion;
            await db.Updateable(version)
                .UpdateColumns(v => new { v.MinSupportVersion })
                .ExecuteCommandAsync(cancellationToken);
        }

        return MapVersion(version);
    }

    /// <summary>
    /// 获取最新的升级迁移历史
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最新的升级迁移历史</returns>
    public async Task<UpgradeMigrationHistory?> GetLatestHistoryAsync(CancellationToken cancellationToken = default)
    {
        var db = GetDbClient();
        var tenantId = GetTenantId();

        var query = db.Queryable<SysMigrationHistory>();
        query = tenantId.HasValue
            ? query.Where(v => v.TenantId == tenantId)
            : query.Where(v => v.TenantId == 0);

        var history = await query.OrderBy(v => v.ExecutedTime, OrderByType.Desc).FirstAsync(cancellationToken);
        return history == null ? null : MapHistory(history);
    }

    /// <summary>
    /// 设置升级中状态
    /// </summary>
    /// <param name="version">升级版本状态</param>
    /// <param name="nodeName">节点名称</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task SetUpgradingAsync(UpgradeVersionState version, string nodeName, DateTime startTime, CancellationToken cancellationToken = default)
    {
        version.IsUpgrading = true;
        version.UpgradeNode = nodeName;
        version.UpgradeStartTime = startTime;

        var db = GetDbClient();
        await db.Updateable<SysVersion>()
            .SetColumns(v => new SysVersion
            {
                IsUpgrading = true,
                UpgradeNode = nodeName,
                UpgradeStartTime = startTime
            })
            .Where(v => v.BasicId == version.Id)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 设置升级完成状态
    /// </summary>
    /// <param name="version">升级版本状态</param>
    /// <param name="appVersion">应用版本</param>
    /// <param name="dbVersion">数据库版本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task SetUpgradeCompletedAsync(UpgradeVersionState version, string appVersion, string dbVersion, CancellationToken cancellationToken = default)
    {
        version.IsUpgrading = false;
        version.AppVersion = appVersion;
        version.DbVersion = dbVersion;

        var db = GetDbClient();
        await db.Updateable<SysVersion>()
            .SetColumns(v => new SysVersion
            {
                IsUpgrading = false,
                AppVersion = appVersion,
                DbVersion = dbVersion
            })
            .Where(v => v.BasicId == version.Id)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 设置升级失败状态
    /// </summary>
    /// <param name="version">升级版本状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task SetUpgradeFailedAsync(UpgradeVersionState version, CancellationToken cancellationToken = default)
    {
        version.IsUpgrading = false;

        var db = GetDbClient();
        await db.Updateable<SysVersion>()
            .SetColumns(v => new SysVersion
            {
                IsUpgrading = false
            })
            .Where(v => v.BasicId == version.Id)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 更新数据库版本
    /// </summary>
    /// <param name="version">升级版本状态</param>
    /// <param name="dbVersion">数据库版本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task UpdateDbVersionAsync(UpgradeVersionState version, string dbVersion, CancellationToken cancellationToken = default)
    {
        version.DbVersion = dbVersion;

        var db = GetDbClient();
        await db.Updateable<SysVersion>()
            .SetColumns(v => new SysVersion
            {
                DbVersion = dbVersion
            })
            .Where(v => v.BasicId == version.Id)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 添加升级迁移历史
    /// </summary>
    /// <param name="history">升级迁移历史</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task AddMigrationHistoryAsync(UpgradeMigrationHistory history, CancellationToken cancellationToken = default)
    {
        var db = GetDbClient();
        var entity = new SysMigrationHistory
        {
            TenantId = history.TenantId ?? 0,
            Version = history.Version,
            ScriptName = history.ScriptName,
            ExecutedTime = history.ExecutedTime,
            Success = history.Success,
            NodeName = history.NodeName,
            ErrorMessage = history.ErrorMessage
        };

        await db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 检查是否存在升级迁移历史
    /// </summary>
    /// <param name="version">版本号</param>
    /// <param name="scriptName">脚本名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<bool> HasMigrationHistoryAsync(string version, string scriptName, CancellationToken cancellationToken = default)
    {
        var db = GetDbClient();
        var tenantId = GetTenantId();

        var query = db.Queryable<SysMigrationHistory>()
            .Where(h => h.Version == version && h.ScriptName == scriptName && h.Success);

        query = tenantId.HasValue
            ? query.Where(h => h.TenantId == tenantId)
            : query.Where(h => h.TenantId == 0);

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 将 SysVersionEntity 映射为 UpgradeVersionState
    /// </summary>
    /// <param name="entity">系统版本实体</param>
    /// <returns>升级版本状态</returns>
    private static UpgradeVersionState MapVersion(SysVersion entity)
    {
        return new UpgradeVersionState
        {
            Id = entity.BasicId,
            TenantId = entity.TenantId,
            AppVersion = entity.AppVersion,
            DbVersion = entity.DbVersion,
            MinSupportVersion = entity.MinSupportVersion,
            IsUpgrading = entity.IsUpgrading,
            UpgradeNode = entity.UpgradeNode,
            UpgradeStartTime = entity.UpgradeStartTime
        };
    }

    /// <summary>
    /// 将 SysMigrationHistory 映射为 UpgradeMigrationHistory
    /// </summary>
    /// <param name="entity">系统迁移历史实体</param>
    /// <returns>升级迁移历史</returns>
    private static UpgradeMigrationHistory MapHistory(SysMigrationHistory entity)
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

    /// <summary>
    /// 获取 SqlSugar 客户端
    /// </summary>
    /// <returns></returns>
    private ISqlSugarClient GetDbClient()
    {
        return _clientResolver.GetClient(_options.ConnectionConfigId);
    }

    /// <summary>
    /// 获取当前租户 ID（如果启用多租户隔离）
    /// </summary>
    /// <returns>租户 ID</returns>
    private long? GetTenantId()
    {
        if (!_options.EnableMultiTenantIsolation)
        {
            return null;
        }

        return _currentTenant.Id;
    }
}
