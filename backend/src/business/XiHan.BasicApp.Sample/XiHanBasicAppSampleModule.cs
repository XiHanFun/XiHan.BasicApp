// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Sample.Domain.Entities;
using XiHan.BasicApp.Sample.Infrastructure.MultiTenancy;
using XiHan.BasicApp.Saas;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Extensions.DependencyInjection;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Tenanting;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Sample;

/// <summary>
/// 曦寒基础应用示例业务模块
/// </summary>
/// <remarks>
/// <para>
/// 这是仓库里最小的一个业务模块，用来演示「在 XiHan.BasicApp 上加一块自己的业务」需要写哪些东西：
/// 一个模块类（本文件）、若干实体、若干仓储，就这些。表由框架在启动时按实体自动建。
/// </para>
/// <para>
/// 同时演示<b>模块分库 × 租户分库</b>两条正交维度：<see cref="SampleNote"/> 未声明模块数据源，跟着租户走；
/// <see cref="SampleErpOrder"/> 标了 <c>[ModuleDataSource("Erp")]</c>，落所在布局下的 Erp 模块库——
/// 平台态与字段隔离租户落共享的 <c>Default_Erp</c>，自带模块库的库隔离租户落自己的 <c>Tenant_{租户Id}_Erp</c>。
/// 启动时会把实际解析结果打印出来，配置是否生效一眼可见。
/// </para>
/// </remarks>
[DependsOn(
    typeof(XiHanBasicAppSaasModule)
)]
public class XiHanBasicAppSampleModule : XiHanModule
{
    /// <summary>
    /// 服务配置：绑定示例配置并接管租户连接提供器
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = services.GetConfiguration();

        services.Configure<SampleTenantModuleDataSourceOptions>(
            configuration.GetSection(SampleTenantModuleDataSourceOptions.SectionName));

        // 装饰 SaaS 的租户连接提供器：租户维度仍由它决定，本模块只在其结果上追加模块库。
        // 不配置任何租户级模块库时它原样透传，等同于没启用。
        services.Replace(ServiceDescriptor.Singleton<ISqlSugarTenantConnectionProvider, SampleTenantConnectionProvider>());
    }

    /// <summary>
    /// 应用初始化：打印示例实体在各租户上下文下解析到的连接，确认二维路由生效
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        using var scope = context.ServiceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<XiHanBasicAppSampleModule>>();
        var clientResolver = sp.GetRequiredService<ISqlSugarClientResolver>();
        var currentTenant = sp.GetRequiredService<ICurrentTenant>();
        var options = sp.GetRequiredService<IOptions<SampleTenantModuleDataSourceOptions>>().Value;

        // 平台态：未声明模块数据源的走主库，声明了的走共享模块库
        LogResolved(logger, clientResolver, "平台态", typeof(SampleNote));
        LogResolved(logger, clientResolver, "平台态", typeof(SampleErpOrder));

        if (options.TenantModuleDataSources.Count == 0)
        {
            logger.LogInformation(
                "未配置租户级模块库（Sample:TenantModuleDataSources），当前所有租户共用同一个 {ModuleDataSource} 模块库。" +
                "要验证二维路由，在配置里为某个租户 Id 补一条模块连接串即可，无需改代码。",
                SampleModuleDataSources.Erp);
            return;
        }

        // 租户态：同一个实体，落库随租户变化——这就是两条维度正交的直接体现
        foreach (var rawTenantId in options.TenantModuleDataSources.Keys)
        {
            if (!long.TryParse(rawTenantId, out var tenantId))
            {
                logger.LogWarning("配置里的租户 Id {TenantId} 不是合法数值，已跳过", rawTenantId);
                continue;
            }

            using var tenantScope = currentTenant.Change(tenantId);
            LogResolved(logger, clientResolver, $"租户 {tenantId}", typeof(SampleNote));
            LogResolved(logger, clientResolver, $"租户 {tenantId}", typeof(SampleErpOrder));
        }
    }

    /// <summary>
    /// 打印某个实体在当前上下文下解析到的连接
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="clientResolver">客户端解析器</param>
    /// <param name="contextName">上下文名称，用于日志区分</param>
    /// <param name="entityType">实体类型</param>
    private static void LogResolved(
        ILogger logger,
        ISqlSugarClientResolver clientResolver,
        string contextName,
        Type entityType)
    {
        try
        {
            var client = clientResolver.GetClientForEntity(entityType);
            logger.LogInformation(
                "[{Context}] {Entity} → 连接 {ConfigId}（{DbType}）",
                contextName,
                entityType.Name,
                client.CurrentConnectionConfig.ConfigId,
                client.CurrentConnectionConfig.DbType);
        }
        catch (Exception ex)
        {
            // 模块数据源解析不到连接时框架 fail-closed 抛异常，这里只记录不中断启动，
            // 免得示例模块的配置缺失拖垮整个应用
            logger.LogWarning(ex, "[{Context}] {Entity} 的连接解析失败", contextName, entityType.Name);
        }
    }
}
