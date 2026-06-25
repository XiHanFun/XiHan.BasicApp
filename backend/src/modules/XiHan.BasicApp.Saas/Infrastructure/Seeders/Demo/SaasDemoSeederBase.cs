#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasDemoSeederBase
// Guid:7f3a1d22-9c0e-4b15-8d44-2a6f5e0b1c3d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/25 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.Demo;

/// <summary>
/// SaaS 演示种子数据基类
/// </summary>
/// <remarks>
/// 演示数据（示例组织/用户/角色、演示业务租户等）与系统基线数据分离，由配置开关
/// <c>Saas:Seed:EnableDemoData</c> 控制是否播种：
/// - 缺省或 true：照常播种（保持「跑全部种子」的现状，适合 Demo 环境）；
/// - 显式 false：所有演示种子整体跳过，库内只保留系统基线数据（适合生产环境）。
/// 切换仅需改配置 + 重启，无需改代码。
/// </remarks>
public abstract class SaasDemoSeederBase : DataSeederBase
{
    /// <summary>
    /// 是否启用演示种子数据的配置键
    /// </summary>
    public const string EnableDemoDataConfigKey = "Saas:Seed:EnableDemoData";

    /// <summary>
    /// 构造函数
    /// </summary>
    protected SaasDemoSeederBase(ISqlSugarClientResolver clientResolver, ILogger logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 是否启用演示数据播种（配置 <see cref="EnableDemoDataConfigKey"/>，缺省/非法值均视为启用）
    /// </summary>
    protected bool IsDemoDataSeedingEnabled()
    {
        var configuration = ServiceProvider.GetService<IConfiguration>();
        var value = configuration?[EnableDemoDataConfigKey];
        // 缺配或无法解析为布尔时按启用处理，保持现有行为；仅显式 false 才关闭
        return !bool.TryParse(value, out var enabled) || enabled;
    }

    /// <summary>
    /// 演示数据未启用时记录日志并返回 true（调用方据此提前结束播种）
    /// </summary>
    protected bool TrySkipWhenDemoDisabled()
    {
        if (IsDemoDataSeedingEnabled())
        {
            return false;
        }

        Logger.LogInformation("演示种子数据已禁用（{ConfigKey}=false），跳过：{SeederName}", EnableDemoDataConfigKey, Name);
        return true;
    }
}
