// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using XiHan.Framework.Data.SqlSugar.Initializers;
using XiHan.Framework.Upgrade.Abstractions;
using XiHan.Framework.Upgrade.Enums;
using XiHan.Framework.Upgrade.Options;

namespace XiHan.BasicApp.Saas.Infrastructure.Upgrade;

/// <summary>
/// 存量库表结构升级：把升级脚本接到数据库初始化的升级段（建表之后、播种之前）
/// </summary>
/// <remarks>
/// 建表只建缺失的表，存量表的新列靠 <c>UpdateScripts</c> 补；升级模块自己是在应用初始化之后才跑脚本的，
/// 那时种子已经按最新实体读过存量表，新列还没补上就会失败。这里让同一台升级引擎先于种子执行；
/// 应用初始化之后升级模块再检查一次，版本已是最新，空转。
/// </remarks>
public sealed class SaasSchemaUpgrader(
    IUpgradeStatusService statusService,
    IUpgradeEngine engine,
    IOptions<XiHanUpgradeOptions> options)
    : IDbSchemaUpgrader
{
    /// <summary>
    /// 执行待执行的升级脚本，失败即中断初始化
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpgradeAsync(CancellationToken cancellationToken = default)
    {
        // 关了启动自动升级就由运维先手工升级再启动，这里不代为执行
        if (!options.Value.EnableAutoCheckOnStartup)
        {
            return;
        }

        await statusService.EnsureInitializedAsync();

        var result = await engine.ExecuteAsync(cancellationToken);
        if (result.Status == UpgradeStatus.Failed)
        {
            throw new InvalidOperationException($"数据库升级失败，已中断启动：{result.Message}");
        }
    }
}
