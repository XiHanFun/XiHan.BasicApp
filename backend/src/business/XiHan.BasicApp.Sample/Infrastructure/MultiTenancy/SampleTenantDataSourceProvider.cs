// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using XiHan.Framework.Data.SqlSugar.Options;
using XiHan.Framework.Data.SqlSugar.Tenanting;

namespace XiHan.BasicApp.Sample.Infrastructure.MultiTenancy;

/// <summary>
/// 示例的租户级数据源提供器：给指定租户的指定数据源指派独立库
/// </summary>
/// <remarks>
/// <para>
/// 这是「模块分库 × 租户分库」二维路由的落地点。框架在解析某个逻辑数据源时先问它：
/// 返回 <c>null</c> → 该租户在这个数据源上没有独立库，落所有租户共享的模块库；
/// 返回描述符 → 框架据此运行时幂等建连并补挂全局过滤器与 AOP。
/// </para>
/// <para>
/// 本示例只读配置、不查库，因此没有递归风险。真实业务若要把映射存进数据库
/// （像 <c>SaasTenantConnectionProvider</c> 那样读 <c>SysTenant</c>），
/// 记得读元数据时切到平台上下文并自行缓存。
/// </para>
/// <para>
/// ConfigId 沿用框架的约定命名 <c>{数据源名}_{租户前缀}{租户Id}</c>，
/// 与静态配置里预置的同名连接不分叉；数据库类型复用共享模块库的设置，少一个配置项。
/// </para>
/// </remarks>
public sealed class SampleTenantDataSourceProvider : ISqlSugarTenantDataSourceProvider
{
    private readonly SampleTenantDataSourceOptions _options;
    private readonly XiHanSqlSugarCoreOptions _sqlSugarOptions;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">示例的租户级数据源配置</param>
    /// <param name="sqlSugarOptions">SqlSugarCore 选项</param>
    public SampleTenantDataSourceProvider(
        IOptions<SampleTenantDataSourceOptions> options,
        IOptions<XiHanSqlSugarCoreOptions> sqlSugarOptions)
    {
        _options = options.Value;
        _sqlSugarOptions = sqlSugarOptions.Value;
    }

    /// <summary>
    /// 解析指定租户在指定数据源上的独立连接描述符
    /// </summary>
    /// <param name="tenantId">当前租户标识</param>
    /// <param name="tenantName">当前租户名称</param>
    /// <param name="dataSourceName">逻辑数据源名</param>
    /// <returns>配置了独立库返回描述符，否则返回 null（落共享模块库）</returns>
    public SqlSugarTenantConnection? Resolve(long tenantId, string? tenantName, string dataSourceName)
    {
        if (!_options.TenantDataSources.TryGetValue(dataSourceName, out var tenants) ||
            !tenants.TryGetValue(tenantId.ToString(), out var connectionString) ||
            string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        // 数据库类型跟随共享模块库；共享库都没配说明配置本身有问题，交给框架 fail-closed
        var sharedConfig = _sqlSugarOptions.ConnectionConfigs
            .Find(config => string.Equals(config.ConfigId, dataSourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"为租户 {tenantId} 配置了数据源 [{dataSourceName}] 的独立库，但缺少同名的共享连接配置，无法确定数据库类型。");

        return new SqlSugarTenantConnection(
            ConfigId: $"{dataSourceName}_{_sqlSugarOptions.TenantConfigIdPrefix}{tenantId}",
            ConnectionString: connectionString,
            DbType: sharedConfig.DbType,
            IsAutoCloseConnection: sharedConfig.IsAutoCloseConnection);
    }
}
