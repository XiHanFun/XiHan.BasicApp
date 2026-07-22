// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// 系统资源种子数据
/// </summary>
public class SysResourceSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysResourceSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysResourceSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级（AI 种子统一在 Order 200+ 独立段，与 Saas/代码生成不交叠；须先于 SysPermissionSeeder）
    /// </summary>
    public override int Order => 201;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]系统资源种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var exists = await client.Queryable<SysResource>().Where(r => r.ResourceCode == "ai").ToListAsync();
        var existsCodes = exists.Select(x => x.ResourceCode).ToHashSet();
        var addList = new List<SysResource>();

        if (!existsCodes.Contains("ai"))
        {
            addList.Add(new SysResource { ResourceCode = "ai", ResourceName = "AI 服务", ResourceType = ResourceType.Api, ResourcePath = "/api/ai", Description = "AI 服务API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = EnableStatus.Enabled, Sort = 402 });
        }

        if (addList.Count == 0)
        {
            Logger.LogInformation("系统资源数据已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个系统资源", addList.Count);
    }
}
