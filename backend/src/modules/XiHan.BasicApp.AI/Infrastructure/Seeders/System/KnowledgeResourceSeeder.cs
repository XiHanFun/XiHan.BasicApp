// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// 知识库资源种子数据
/// </summary>
public class KnowledgeResourceSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public KnowledgeResourceSeeder(ISqlSugarClientResolver clientResolver, ILogger<KnowledgeResourceSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级（RAG 段 205+，晚于 AI provider 段 200-204；须先于知识库权限种子）
    /// </summary>
    public override int Order => 205;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]知识库资源种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var exists = await client.Queryable<SysResource>().Where(r => r.ResourceCode == "knowledge_base").ToListAsync();
        var existsCodes = exists.Select(x => x.ResourceCode).ToHashSet();
        var addList = new List<SysResource>();

        if (!existsCodes.Contains("knowledge_base"))
        {
            addList.Add(new SysResource { ResourceCode = "knowledge_base", ResourceName = "知识库", ResourceType = ResourceType.Api, ResourcePath = "/api/knowledge-document", Description = "RAG 知识库API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = EnableStatus.Enabled, Sort = 403 });
        }

        if (addList.Count == 0)
        {
            Logger.LogInformation("知识库资源数据已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个知识库资源", addList.Count);
    }
}
