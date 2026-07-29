// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// AI 助手权限种子数据
/// </summary>
public class AssistantPermissionSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public AssistantPermissionSeeder(ISqlSugarClientResolver clientResolver, ILogger<AssistantPermissionSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级（须先于助手菜单种子）
    /// </summary>
    public override int Order => 214;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]助手权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var resource = await client.Queryable<SysResource>().FirstAsync(r => r.ResourceCode == "ai_assistant");
        var operations = await client.Queryable<SysOperation>().ToListAsync();
        if (resource is null || operations.Count == 0)
        {
            Logger.LogWarning("AI 助手资源或系统操作不存在，跳过助手权限种子数据");
            return;
        }

        var operationMap = operations.ToDictionary(o => o.OperationCode, o => o);
        string[] operationCodes = ["read", "create", "update", "delete"];
        var permissionCodes = operationCodes.Select(op => $"ai_assistant:{op}").ToList();
        var existingCodes = (await client.Queryable<SysPermission>().Where(p => permissionCodes.Contains(p.PermissionCode)).ToListAsync())
            .Select(p => p.PermissionCode)
            .ToHashSet();

        var addList = new List<SysPermission>();
        foreach (var operationCode in operationCodes)
        {
            var permissionCode = $"ai_assistant:{operationCode}";
            if (existingCodes.Contains(permissionCode) || !operationMap.TryGetValue(operationCode, out var operation))
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
                Tags = "ai_assistant",
                Status = EnableStatus.Enabled,
                Sort = 940 + addList.Count
            });
        }

        if (addList.Count == 0)
        {
            Logger.LogInformation("AI 助手权限数据已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个 AI 助手权限", addList.Count);
    }
}
