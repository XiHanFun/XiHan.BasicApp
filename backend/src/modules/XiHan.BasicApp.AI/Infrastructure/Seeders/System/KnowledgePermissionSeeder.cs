#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:KnowledgePermissionSeeder
// Guid:20aee395-095f-4778-b0df-6dda2409656e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// 知识库权限种子数据
/// </summary>
public class KnowledgePermissionSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public KnowledgePermissionSeeder(ISqlSugarClientResolver clientResolver, ILogger<KnowledgePermissionSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级（须先于知识库菜单种子）
    /// </summary>
    public override int Order => 206;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]知识库权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var resources = await client.Queryable<SysResource>().Where(r => r.ResourceCode == "knowledge_base").ToListAsync();
        var operations = await client.Queryable<SysOperation>().ToListAsync();
        if (resources.Count == 0 || operations.Count == 0)
        {
            Logger.LogWarning("知识库资源或系统操作不存在，跳过知识库权限种子数据");
            return;
        }

        var operationMap = operations.ToDictionary(o => o.OperationCode, o => o);
        var target = new Dictionary<string, string[]>
        {
            ["knowledge_base"] = ["read", "create", "update", "delete", "execute"]
        };
        var permissionCodes = target.SelectMany(kv => kv.Value.Select(op => $"{kv.Key}:{op}")).ToList();
        var existing = await client.Queryable<SysPermission>().Where(p => permissionCodes.Contains(p.PermissionCode)).ToListAsync();
        var existingCodes = existing.Select(x => x.PermissionCode).ToHashSet();
        var addList = new List<SysPermission>();

        foreach (var resource in resources)
        {
            if (!target.TryGetValue(resource.ResourceCode, out var opCodes))
            {
                continue;
            }

            foreach (var opCode in opCodes)
            {
                var permissionCode = $"{resource.ResourceCode}:{opCode}";
                if (existingCodes.Contains(permissionCode) || !operationMap.TryGetValue(opCode, out var operation))
                {
                    continue;
                }

                addList.Add(new SysPermission
                {
                    ResourceId = resource.BasicId,
                    OperationId = operation.BasicId,
                    PermissionCode = permissionCode,
                    PermissionName = $"{resource.ResourceName}-{operation.OperationName}",
                    PermissionDescription = $"对{resource.ResourceName}执行{operation.OperationName}操作",
                    IsRequireAudit = operation.IsRequireAudit,
                    Tags = "knowledge_base",
                    Status = EnableStatus.Enabled,
                    Sort = 920 + addList.Count
                });
            }
        }

        if (addList.Count == 0)
        {
            Logger.LogInformation("知识库权限数据已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个知识库权限", addList.Count);
    }
}
