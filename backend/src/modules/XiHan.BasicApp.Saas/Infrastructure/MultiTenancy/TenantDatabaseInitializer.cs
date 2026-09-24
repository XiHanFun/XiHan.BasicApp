// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Initializers;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Upgrade.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.MultiTenancy;

/// <summary>
/// 租户数据库初始化器实现
/// </summary>
/// <remarks>
/// 切到目标租户上下文后复用框架 <see cref="IDbInitializer"/>：运行时连接提供器将其解析到该租户独立库，
/// 完成建库/建表。独立库只建租户数据的表（平台目录、账号与授权、读共享模板固定在平台库），本应用的种子只播平台库。
/// 建好的库按当前实体建表、本就是最新结构，随即登记升级基线，之后的升级只跑更新的脚本。
/// DDL 不能在事务内执行，调用方（AppService）不得包裹事务型工作单元。
/// 配置状态（Configuring/Configured/Failed）写回平台库 SysTenant，在租户上下文切换之外进行。
/// </remarks>
public sealed class TenantDatabaseInitializer : ITenantDatabaseInitializer
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IDbInitializer _dbInitializer;
    private readonly IUpgradeEngine _upgradeEngine;
    private readonly ITenantConnectionCacheInvalidator _connectionCacheInvalidator;
    private readonly ILogger<TenantDatabaseInitializer> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantDatabaseInitializer(
        ITenantRepository tenantRepository,
        ICurrentTenant currentTenant,
        IDbInitializer dbInitializer,
        IUpgradeEngine upgradeEngine,
        ITenantConnectionCacheInvalidator connectionCacheInvalidator,
        ILogger<TenantDatabaseInitializer> logger)
    {
        _tenantRepository = tenantRepository;
        _currentTenant = currentTenant;
        _dbInitializer = dbInitializer;
        _upgradeEngine = upgradeEngine;
        _connectionCacheInvalidator = connectionCacheInvalidator;
        _logger = logger;
    }

    /// <summary>
    /// 为库隔离租户初始化独立数据库：建库 → 建表 → 登记升级基线（幂等）。
    /// </summary>
    /// <param name="tenantId">租户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>初始化后的租户（含最新配置状态）</returns>
    public async Task<SysTenant> InitializeAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tenantId), "租户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException("租户不存在。");

        if (tenant.IsolationMode != TenantIsolationMode.Database)
        {
            throw new InvalidOperationException("仅库隔离（Database）租户需要初始化独立数据库。");
        }

        if (string.IsNullOrWhiteSpace(tenant.ConnectionString))
        {
            throw new InvalidOperationException("租户尚未配置数据库连接字符串，无法初始化。");
        }

        // 使运行时连接提供器读取最新配置（如刚设置/变更连接串）
        _connectionCacheInvalidator.Invalidate(tenantId);

        tenant.MarkConfigStatus(TenantConfigStatus.Configuring);
        _ = await _tenantRepository.UpdateAsync(tenant, cancellationToken);

        try
        {
            // 切到目标租户上下文：DbInitializer 经运行时连接提供器解析到该租户独立库
            using (_currentTenant.Change(tenantId, tenant.TenantName))
            {
                // 整套布局一起初始化：租户主库，加上该租户自带的模块库（Tenant_{id}_Erp 这类）
                await _dbInitializer.InitializeCurrentLayoutAsync();

                // 新库不从 0.0.0 补跑历史脚本（它们改的表多半不在租户库里）；已有版本记录的库（重跑初始化）保持不动
                _ = await _upgradeEngine.BaselineAsync(cancellationToken);
            }

            tenant.MarkConfigStatus(TenantConfigStatus.Configured);
            return await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "租户 {TenantId} 数据库初始化失败。", tenantId);
            tenant.MarkConfigStatus(TenantConfigStatus.Failed);
            _ = await _tenantRepository.UpdateAsync(tenant, cancellationToken);
            throw;
        }
    }
}
