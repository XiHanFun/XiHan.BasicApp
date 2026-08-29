// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
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
/// 同时演示<b>模块分库</b>：<see cref="SampleNote"/> 未声明数据源落主库，
/// <see cref="SampleErpOrder"/> 标了 <c>[DataSource("Erp")]</c> 落 Erp 库，
/// 租户上下文保持统一。启动时会打印一次实际解析结果，便于确认配置是否生效。
/// </para>
/// </remarks>
[DependsOn(
    typeof(XiHanBasicAppSaasModule)
)]
public class XiHanBasicAppSampleModule : XiHanModule
{
    /// <summary>
    /// 应用初始化：打印两个示例实体各自解析到的连接，确认模块分库配置生效
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        using var scope = context.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<XiHanBasicAppSampleModule>>();
        var clientResolver = scope.ServiceProvider.GetRequiredService<ISqlSugarClientResolver>();

        foreach (var entityType in new[] { typeof(SampleNote), typeof(SampleErpOrder) })
        {
            try
            {
                var client = clientResolver.GetClientForEntity(entityType);
                logger.LogInformation(
                    "示例实体 {Entity} 解析到连接 {ConfigId}（{DbType}）",
                    entityType.Name,
                    client.CurrentConnectionConfig.ConfigId,
                    client.CurrentConnectionConfig.DbType);
            }
            catch (Exception ex)
            {
                // 声明的 ConfigId 没有对应连接时框架 fail-closed 抛异常，这里只记录不中断启动，
                // 让示例模块的配置缺失不至于拖垮整个应用
                logger.LogWarning(ex, "示例实体 {Entity} 的数据源解析失败", entityType.Name);
            }
        }
    }
}
