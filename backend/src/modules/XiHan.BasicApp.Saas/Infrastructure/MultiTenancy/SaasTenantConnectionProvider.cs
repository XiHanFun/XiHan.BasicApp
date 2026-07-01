#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasTenantConnectionProvider
// Guid:3a8e5d21-7c94-4f60-b1d8-6e2a9c4f0b53
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Collections.Concurrent;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Options;
using XiHan.Framework.Data.SqlSugar.Tenanting;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.MultiTenancy;

/// <summary>
/// SaaS 租户连接提供器（库隔离桥接：SysTenant → 运行时 SqlSugar 连接）
/// </summary>
/// <remarks>
/// 由框架 <see cref="ISqlSugarTenantConnectionProvider"/> 在解析当前租户客户端时咨询：
/// <list type="bullet">
///   <item><see cref="TenantIsolationMode.Field"/>：返回 <c>null</c>，走平台库 + 行过滤器（现状）；</item>
///   <item><see cref="TenantIsolationMode.Database"/>：解密连接串，返回 <c>Tenant_{id}</c> 连接描述符，框架据此运行时建连；</item>
///   <item><see cref="TenantIsolationMode.Schema"/>：抛异常（尚未实装，fail-closed，杜绝静默退化）。</item>
/// </list>
/// 读取租户元数据时切到平台上下文（<c>ICurrentTenant.Change(null)</c>）以走默认连接并避免递归；结果按租户缓存，
/// 隔离配置变更须经 <see cref="ITenantConnectionCacheInvalidator"/> 失效。
/// </remarks>
public sealed class SaasTenantConnectionProvider : ISqlSugarTenantConnectionProvider, ITenantConnectionCacheInvalidator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITenantConnectionSecretProtector _secretProtector;
    private readonly ILogger<SaasTenantConnectionProvider> _logger;

    // 与框架静态 ConfigId 解析共用同一前缀，避免运行时建连与预置连接命名分叉
    private readonly string _tenantConfigIdPrefix;

    // 值为 null 表示「该租户无需独立连接」的已缓存结论；异常不入缓存，下次请求重试（保持 fail-closed）
    private readonly ConcurrentDictionary<long, SqlSugarTenantConnection?> _cache = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasTenantConnectionProvider(
        IServiceScopeFactory scopeFactory,
        ITenantConnectionSecretProtector secretProtector,
        IOptions<XiHanSqlSugarCoreOptions> sqlSugarOptions,
        ILogger<SaasTenantConnectionProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _secretProtector = secretProtector;
        _tenantConfigIdPrefix = sqlSugarOptions.Value.TenantConfigIdPrefix;
        _logger = logger;
    }

    /// <inheritdoc />
    public SqlSugarTenantConnection? Resolve(long tenantId, string? tenantName)
    {
        if (_cache.TryGetValue(tenantId, out var cached))
        {
            return cached;
        }

        var descriptor = LoadDescriptor(tenantId);
        _cache[tenantId] = descriptor;
        return descriptor;
    }

    /// <inheritdoc />
    public void Invalidate(long tenantId)
    {
        _cache.TryRemove(tenantId, out _);
    }

    private SqlSugarTenantConnection? LoadDescriptor(long tenantId)
    {
        SysTenant? tenant;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var currentTenant = serviceProvider.GetRequiredService<ICurrentTenant>();

            // 平台上下文读取租户元数据：走默认连接、无租户过滤、避免递归回本提供器
            using (currentTenant.Change(null))
            {
                var client = serviceProvider.GetRequiredService<ISqlSugarClientResolver>().GetCurrentClient();
                tenant = client.Queryable<SysTenant>().Where(x => x.BasicId == tenantId).First();
            }
        }
        catch (Exception ex)
        {
            // 读取失败（如初始化期 SysTenant 表尚未建立）：回退默认连接，不阻断启动/请求
            _logger.LogWarning(ex, "读取租户 {TenantId} 连接元数据失败，回退默认连接。", tenantId);
            return null;
        }

        if (tenant is null)
        {
            return null;
        }

        switch (tenant.IsolationMode)
        {
            case TenantIsolationMode.Field:
                return null;

            case TenantIsolationMode.Schema:
                throw new NotSupportedException($"租户 {tenantId} 声明 Schema 隔离，但当前尚未实装 Schema 隔离（fail-closed，拒绝退化为行隔离）。");

            case TenantIsolationMode.Database:
                var connectionString = _secretProtector.Unprotect(tenant.ConnectionString);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException($"租户 {tenantId} 声明库隔离但连接字符串为空（fail-closed）。");
                }

                var dbType = MapDbType(tenant.DatabaseType);
                return new SqlSugarTenantConnection($"{_tenantConfigIdPrefix}{tenantId}", connectionString, dbType);

            default:
                return null;
        }
    }

    private static DbType MapDbType(TenantDatabaseType? databaseType)
    {
        return databaseType switch
        {
            TenantDatabaseType.SqlServer => DbType.SqlServer,
            TenantDatabaseType.MySql => DbType.MySql,
            TenantDatabaseType.PostgreSql => DbType.PostgreSQL,
            TenantDatabaseType.SQLite => DbType.Sqlite,
            TenantDatabaseType.Oracle => DbType.Oracle,
            _ => throw new InvalidOperationException("库隔离租户必须指定有效的数据库类型（DatabaseType）。")
        };
    }
}
