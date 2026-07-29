// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// AI 助手资源种子数据
/// </summary>
public class AssistantResourceSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public AssistantResourceSeeder(ISqlSugarClientResolver clientResolver, ILogger<AssistantResourceSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级（助手段 213+，晚于提示词库 209-212；须先于权限种子）
    /// </summary>
    public override int Order => 213;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]助手资源种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var exists = await client.Queryable<SysResource>().Where(r => r.ResourceCode == "ai_assistant").ToListAsync();
        if (exists.Count > 0)
        {
            Logger.LogInformation("AI 助手资源数据已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(new List<SysResource>
        {
            new() { ResourceCode = "ai_assistant", ResourceName = "AI 助手", ResourceType = ResourceType.Api, ResourcePath = "/api/ai-assistant", Description = "AI 助手配置API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = EnableStatus.Enabled, Sort = 405 }
        });
        Logger.LogInformation("成功初始化 AI 助手资源");
    }
}
