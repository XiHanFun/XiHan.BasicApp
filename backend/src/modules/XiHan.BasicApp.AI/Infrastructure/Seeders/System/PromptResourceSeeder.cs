#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PromptResourceSeeder
// Guid:a11c0de0-a001-4a10-9a00-00000000aia0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/06 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// AI 提示词库资源种子数据
/// </summary>
public class PromptResourceSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PromptResourceSeeder(ISqlSugarClientResolver clientResolver, ILogger<PromptResourceSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级（提示词库段 209+，晚于知识库 205-208；须先于权限种子）
    /// </summary>
    public override int Order => 209;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]提示词库资源种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var exists = await client.Queryable<SysResource>().Where(r => r.ResourceCode == "ai_prompt").ToListAsync();
        var existsCodes = exists.Select(x => x.ResourceCode).ToHashSet();
        var addList = new List<SysResource>();

        if (!existsCodes.Contains("ai_prompt"))
        {
            addList.Add(new SysResource { ResourceCode = "ai_prompt", ResourceName = "AI 提示词", ResourceType = ResourceType.Api, ResourcePath = "/api/ai-prompt", Description = "AI 提示词库API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = EnableStatus.Enabled, Sort = 404 });
        }

        if (addList.Count == 0)
        {
            Logger.LogInformation("AI 提示词库资源数据已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个 AI 提示词库资源", addList.Count);
    }
}
