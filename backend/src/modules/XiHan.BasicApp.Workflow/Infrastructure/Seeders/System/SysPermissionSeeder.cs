#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionSeeder
// Guid:d17e50b9-42c6-4a38-b5e0-76d93c28f4a1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:34:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Workflow.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Workflow.Infrastructure.Seeders.System;

/// <summary>
/// 系统权限种子数据（资源 × 操作 → workflow:* 权限）
/// </summary>
public class SysPermissionSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysPermissionSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysPermissionSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级（须先于 SysMenuSeeder，菜单建立时即可解析 workflow:read 绑定可见性）
    /// </summary>
    public override int Order => 302;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Workflow]系统权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var resources = await client.Queryable<SysResource>()
            .Where(r => r.ResourceCode == WorkflowPermissionCodes.Resource)
            .ToListAsync();
        var operations = await client.Queryable<SysOperation>().ToListAsync();
        if (resources.Count == 0 || operations.Count == 0)
        {
            Logger.LogWarning("系统资源或操作不存在，跳过系统权限种子数据");
            return;
        }

        var operationMap = operations.ToDictionary(o => o.OperationCode, o => o);
        var target = new Dictionary<string, string[]>
        {
            [WorkflowPermissionCodes.Resource] = ["read", "create", "update", "delete", "execute"]
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
                    Tags = "workflow",
                    Status = EnableStatus.Enabled,
                    Sort = 930 + addList.Count
                });
            }
        }

        if (addList.Count == 0)
        {
            Logger.LogInformation("系统权限数据已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个系统权限", addList.Count);
    }
}
