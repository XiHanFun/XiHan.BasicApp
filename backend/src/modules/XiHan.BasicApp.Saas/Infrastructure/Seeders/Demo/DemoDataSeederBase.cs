// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 演示种子基类：配置开关 <c>Saas:Seed:EnableDemoData</c> 为 true 时才写，缺省或 false 一律不写
/// </summary>
/// <remarks>
/// 演示数据只是示例：系统运行所需的基础数据由其它种子负责，与开关无关。
/// 演示数据只在第一次写入，之后改过、删过的都不再覆盖或补回——拿它随便试。
/// 开关写错（不是布尔值）直接报错，不按关闭处理。
/// </remarks>
public abstract class DemoDataSeederBase(
    ISqlSugarClientResolver clientResolver,
    ILogger logger,
    IServiceProvider serviceProvider)
    : PlatformDataSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 演示数据开关的配置键
    /// </summary>
    public const string EnableDemoDataKey = "Saas:Seed:EnableDemoData";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected sealed override async Task SeedInternalAsync()
    {
        if (!ServiceProvider.GetRequiredService<IConfiguration>().GetValue<bool>(EnableDemoDataKey))
        {
            Logger.LogInformation("{Seeder}：演示数据未开启（{Key}），跳过", Name, EnableDemoDataKey);
            return;
        }

        await SeedDemoAsync();
    }

    /// <summary>
    /// 写入演示数据（开关开启时）
    /// </summary>
    protected abstract Task SeedDemoAsync();
}
