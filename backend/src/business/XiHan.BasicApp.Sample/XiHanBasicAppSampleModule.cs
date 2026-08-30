// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Sample.Domain.Entities;
using XiHan.BasicApp.Saas;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Data.SqlSugar.Clients;

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
/// 平台态与字段隔离租户落 <c>Default_Erp</c>，库隔离租户落它自己的 <c>Tenant_{租户Id}_Erp</c>。
/// 后者由框架按约定派生，业务侧不写一行代码、也不加一条配置。
/// 启动时会把实际解析结果打印出来，路由是否生效一眼可见。
/// </para>
/// </remarks>
[DependsOn(
    typeof(XiHanBasicAppSaasModule)
)]
public class XiHanBasicAppSampleModule : XiHanModule
{
    /// <summary>
    /// 应用初始化：打印示例实体在平台态下解析到的连接，确认模块分库生效
    /// </summary>
    /// <remarks>
    /// 租户维度不在这里打印：库隔离租户的连接是运行期按需建的，启动时还没有它的上下文。
    /// 要看某个租户的解析结果，登录进那个租户后看请求日志即可。
    /// </remarks>
    /// <param name="context">应用初始化上下文</param>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        using var scope = context.ServiceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<XiHanBasicAppSampleModule>>();
        var clientResolver = sp.GetRequiredService<ISqlSugarClientResolver>();

        // 平台态：未声明模块数据源的走主库，声明了的走模块库
        LogResolved(logger, clientResolver, "平台态", typeof(SampleNote));
        LogResolved(logger, clientResolver, "平台态", typeof(SampleErpOrder));
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
