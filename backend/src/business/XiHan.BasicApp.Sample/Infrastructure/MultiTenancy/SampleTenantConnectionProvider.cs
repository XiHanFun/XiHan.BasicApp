// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Infrastructure.MultiTenancy;
using XiHan.Framework.Data.SqlSugar.Options;
using XiHan.Framework.Data.SqlSugar.Tenanting;

namespace XiHan.BasicApp.Sample.Infrastructure.MultiTenancy;

/// <summary>
/// 示例的租户连接提供器：在 SaaS 库隔离租户的连接上，按配置补挂该租户自己的模块库
/// </summary>
/// <remarks>
/// <para>
/// 这是「模块分库 × 租户分库」二维路由的落地点，也是这两条维度唯一需要交汇的地方。
/// <see cref="SaasTenantConnectionProvider"/> 只管租户维度——按 <c>SysTenant.IsolationMode</c>
/// 决定这个租户有没有独立主库；本类在它给出的描述符上追加模块库，让「租户主库独立、
/// 模块库也独立」这种组合可以被表达出来。
/// </para>
/// <para>
/// 一条描述符给出的是这个租户<b>一整套</b>数据库布局：主库加上若干模块库。没在里面声明的模块
/// 会回落到默认布局下的共享模块库，所以「租户主库独立、模块库仍共享」不需要额外配置，是默认行为。
/// </para>
/// <para>
/// 模块库的连接标识由框架从主库标识派生（<c>Tenant_{租户Id}_Erp</c>），未填的字段继承租户主库，
/// 所以这里只写模块名和连接串。
/// </para>
/// </remarks>
public sealed class SampleTenantConnectionProvider : ISqlSugarTenantConnectionProvider
{
    private readonly SaasTenantConnectionProvider _inner;
    private readonly SampleTenantModuleDataSourceOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="inner">SaaS 租户连接提供器（负责租户维度）</param>
    /// <param name="options">示例的租户级模块库配置</param>
    public SampleTenantConnectionProvider(
        SaasTenantConnectionProvider inner,
        IOptions<SampleTenantModuleDataSourceOptions> options)
    {
        _inner = inner;
        _options = options.Value;
    }

    /// <summary>
    /// 解析指定租户的独立连接描述符
    /// </summary>
    /// <param name="tenantId">当前租户标识</param>
    /// <param name="tenantName">当前租户名称</param>
    /// <returns>需要独立连接时返回描述符，否则返回 null</returns>
    public SqlSugarTenantConnection? Resolve(long tenantId, string? tenantName)
    {
        var descriptor = _inner.Resolve(tenantId, tenantName);

        // 该租户没有独立主库（字段隔离）：模块表落共享模块库，没有可追加的地方
        if (descriptor is null)
        {
            return null;
        }

        if (!_options.TenantModuleDataSources.TryGetValue(tenantId.ToString(), out var moduleConnectionStrings) ||
            moduleConnectionStrings.Count == 0)
        {
            return descriptor;
        }

        var moduleConfigs = moduleConnectionStrings
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => new SqlSugarModuleDataSourceConfigOptions
            {
                ModuleDataSource = pair.Key,
                ConnectionString = pair.Value
            })
            .ToList();

        return moduleConfigs.Count == 0
            ? descriptor
            : descriptor with { ModuleDataSourceConfigs = moduleConfigs };
    }
}
