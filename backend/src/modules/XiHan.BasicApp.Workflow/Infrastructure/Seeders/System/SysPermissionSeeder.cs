// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Workflow.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

namespace XiHan.BasicApp.Workflow.Infrastructure.Seeders.System;

/// <summary>
/// 系统权限种子数据（资源 × 操作 → workflow:* 权限）
/// </summary>
public class SysPermissionSeeder : PlatformDataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysPermissionSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysPermissionSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级（须先于 WorkflowMenuSeeder，菜单建立时即可解析 workflow:read 绑定可见性）
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

        var operationMap = BuildOperationMap(operations);
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
                    ModuleCode = WorkflowPermissionCodes.Module,
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

    /// <summary>
    /// 按操作编码收敛操作字典（同编码多行时取一条并告警）
    /// </summary>
    /// <remarks>
    /// 操作表的唯一约束是 (TenantId, OperationCode, IsDeleted)，同一编码天然可以在多个租户下并存；
    /// 而这里的查询没有租户条件，直接 ToDictionary 会以「An item with the same key has already been added」
    /// 让整个播种失败，权限/菜单/角色授权整条链在干净库上一起断掉。
    /// 因此改为分组取一条：优先取平台租户（TenantId = 0）的那行——它才是 <see cref="SysOperationSeeder"/>
    /// 播下的动作模板；同租户内再按主键取最早的一条保证结果稳定，并对重复编码记一条 Warning 供排查。
    /// 不直接过滤掉非平台行，是因为操作种子按编码去重，租户先建了同名操作时平台行根本不会被插入，
    /// 硬过滤会让操作字典变空、权限一条都播不出来。
    /// </remarks>
    /// <param name="operations">操作表全量记录</param>
    /// <returns>操作编码到操作的映射</returns>
    private Dictionary<string, SysOperation> BuildOperationMap(IEnumerable<SysOperation> operations)
    {
        var map = new Dictionary<string, SysOperation>(StringComparer.Ordinal);
        var groups = operations
            .Where(o => !string.IsNullOrWhiteSpace(o.OperationCode))
            .GroupBy(o => o.OperationCode, StringComparer.Ordinal);
        foreach (var group in groups)
        {
            var candidates = group
                .OrderBy(o => o.TenantId == 0 ? 0 : 1)
                .ThenBy(o => o.BasicId)
                .ToList();
            map[group.Key] = candidates[0];
            if (candidates.Count > 1)
            {
                Logger.LogWarning(
                    "操作编码 {OperationCode} 存在 {Count} 行记录，已取主键 {BasicId}（租户 {TenantId}）的一条派生工作流权限",
                    group.Key,
                    candidates.Count,
                    candidates[0].BasicId,
                    candidates[0].TenantId);
            }
        }

        return map;
    }
}
