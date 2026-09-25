// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 权限目录种子基类：按声明写入一个模块的资源与权限（平台数据，TenantId = 0）
/// </summary>
/// <remarks>
/// 目录由代码定义：已有的资源与权限对齐名称、说明、作用侧、审计、排序等元数据，缺的按声明插入。
/// 启停归运营，只在插入时启用；声明之外的行不删（下线的权限由升级脚本连同引用一起清理）。
/// 资源型权限引用的操作来自平台操作字典，字典里没有就直接报错——说明种子顺序或声明写错了，不能跳过了事。
/// </remarks>
public abstract class PermissionCatalogSeederBase(
    ISqlSugarClientResolver clientResolver,
    ILogger logger,
    IServiceProvider serviceProvider)
    : PlatformDataSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子行的备注
    /// </summary>
    private const string SeededRemark = "系统初始化内置权限";

    /// <summary>
    /// 模块编码（权限的模块归属，权限页按模块筛选）
    /// </summary>
    public abstract string ModuleCode { get; }

    /// <summary>
    /// 本模块的资源（只有资源型权限需要）
    /// </summary>
    public virtual IReadOnlyList<ResourceSeed> Resources => [];

    /// <summary>
    /// 本模块的权限
    /// </summary>
    public abstract IReadOnlyList<PermissionSeed> Permissions { get; }

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var resourceIds = await SeedResourcesAsync();
        var operationIds = await LoadOperationIdsAsync();
        await SeedPermissionsAsync(resourceIds, operationIds);
    }

    /// <summary>
    /// 权限标签：[模块, 分组]
    /// </summary>
    public string BuildTags(PermissionSeed seed)
    {
        return JsonSerializer.Serialize(new[] { ModuleCode, seed.Group });
    }

    private async Task<Dictionary<string, long>> SeedResourcesAsync()
    {
        var codes = Resources.Select(static resource => resource.Code).ToList();
        if (codes.Count == 0)
        {
            return new Dictionary<string, long>(StringComparer.Ordinal);
        }

        var existing = (await DbClient.Queryable<SysResource>()
                .Where(resource => resource.TenantId == 0 && codes.Contains(resource.ResourceCode))
                .ToListAsync())
            .ToDictionary(resource => resource.ResourceCode, StringComparer.Ordinal);

        var adding = new List<SysResource>();
        var updated = 0;
        foreach (var seed in Resources)
        {
            if (existing.TryGetValue(seed.Code, out var resource))
            {
                if (ApplyResource(resource, seed))
                {
                    _ = await DbClient.Updateable(resource).ExecuteCommandAsync();
                    updated++;
                }

                continue;
            }

            resource = new SysResource { ResourceCode = seed.Code, Status = EnableStatus.Enabled };
            _ = ApplyResource(resource, seed);
            adding.Add(resource);
        }

        if (adding.Count > 0)
        {
            await BulkInsertAsync(adding);
        }

        if (adding.Count > 0 || updated > 0)
        {
            Logger.LogInformation("{Seeder}：资源新增 {AddCount} 个、对齐 {UpdateCount} 个", Name, adding.Count, updated);
        }

        return (await DbClient.Queryable<SysResource>()
                .Where(resource => resource.TenantId == 0 && codes.Contains(resource.ResourceCode))
                .ToListAsync())
            .ToDictionary(resource => resource.ResourceCode, resource => resource.BasicId, StringComparer.Ordinal);
    }

    private async Task<Dictionary<string, long>> LoadOperationIdsAsync()
    {
        var codes = Permissions
            .Where(static permission => permission.Operation is not null)
            .Select(static permission => permission.Operation!.Code)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (codes.Count == 0)
        {
            return new Dictionary<string, long>(StringComparer.Ordinal);
        }

        var operationIds = (await DbClient.Queryable<SysOperation>()
                .Where(operation => operation.TenantId == 0 && codes.Contains(operation.OperationCode))
                .ToListAsync())
            .ToDictionary(operation => operation.OperationCode, operation => operation.BasicId, StringComparer.Ordinal);

        var missing = codes.Where(code => !operationIds.ContainsKey(code)).ToList();
        return missing.Count == 0
            ? operationIds
            : throw new InvalidOperationException($"{Name}：操作字典里没有 {string.Join("、", missing)}，操作字典种子须先于权限目录执行。");
    }

    private async Task SeedPermissionsAsync(IReadOnlyDictionary<string, long> resourceIds, IReadOnlyDictionary<string, long> operationIds)
    {
        var codes = Permissions.Select(static permission => permission.Code).ToList();
        var existing = (await DbClient.Queryable<SysPermission>()
                .Where(permission => permission.TenantId == 0 && codes.Contains(permission.PermissionCode))
                .ToListAsync())
            .ToDictionary(permission => permission.PermissionCode, StringComparer.Ordinal);

        var adding = new List<SysPermission>();
        var updated = 0;
        foreach (var seed in Permissions)
        {
            var resourceId = seed.Resource is null ? (long?)null : resourceIds[seed.Resource.Code];
            var operationId = seed.Operation is null ? (long?)null : operationIds[seed.Operation.Code];
            if (existing.TryGetValue(seed.Code, out var permission))
            {
                if (ApplyPermission(permission, seed, resourceId, operationId))
                {
                    _ = await DbClient.Updateable(permission).ExecuteCommandAsync();
                    updated++;
                }

                continue;
            }

            permission = new SysPermission { PermissionCode = seed.Code, Status = EnableStatus.Enabled };
            _ = ApplyPermission(permission, seed, resourceId, operationId);
            adding.Add(permission);
        }

        if (adding.Count > 0)
        {
            await BulkInsertAsync(adding);
        }

        Logger.LogInformation("{Seeder}：权限新增 {AddCount} 个、对齐 {UpdateCount} 个", Name, adding.Count, updated);
    }

    private static bool ApplyResource(SysResource resource, ResourceSeed seed)
    {
        var changed = false;
        changed |= SeedValues.SetIfChanged(resource.ResourceName, seed.Name, value => resource.ResourceName = value);
        changed |= SeedValues.SetIfChanged(resource.ResourceType, ResourceType.Api, value => resource.ResourceType = value);
        changed |= SeedValues.SetIfChanged(resource.ResourcePath, seed.Path, value => resource.ResourcePath = value);
        changed |= SeedValues.SetIfChanged(resource.Description, seed.Description, value => resource.Description = value);
        changed |= SeedValues.SetIfChanged(resource.AccessLevel, ResourceAccessLevel.Authorized, value => resource.AccessLevel = value);
        changed |= SeedValues.SetIfChanged(resource.Sort, seed.Sort, value => resource.Sort = value);
        changed |= SeedValues.SetIfChanged(resource.Remark, SeededRemark, value => resource.Remark = value);
        return changed;
    }

    private bool ApplyPermission(SysPermission permission, PermissionSeed seed, long? resourceId, long? operationId)
    {
        var changed = false;
        changed |= SeedValues.SetIfChanged(permission.PermissionType, seed.Resource is null ? PermissionType.Functional : PermissionType.ResourceBased, value => permission.PermissionType = value);
        changed |= SeedValues.SetIfChanged(permission.ResourceId, resourceId, value => permission.ResourceId = value);
        changed |= SeedValues.SetIfChanged(permission.OperationId, operationId, value => permission.OperationId = value);
        changed |= SeedValues.SetIfChanged(permission.ModuleCode, ModuleCode, value => permission.ModuleCode = value);
        changed |= SeedValues.SetIfChanged(permission.PermissionName, seed.Name, value => permission.PermissionName = value);
        changed |= SeedValues.SetIfChanged(permission.PermissionDescription, seed.Description, value => permission.PermissionDescription = value);
        changed |= SeedValues.SetIfChanged(permission.Tags, BuildTags(seed), value => permission.Tags = value);
        changed |= SeedValues.SetIfChanged(permission.Side, seed.Side, value => permission.Side = value);
        changed |= SeedValues.SetIfChanged(permission.IsRequireAudit, seed.IsRequireAudit, value => permission.IsRequireAudit = value);
        changed |= SeedValues.SetIfChanged(permission.Priority, seed.Sort, value => permission.Priority = value);
        changed |= SeedValues.SetIfChanged(permission.Sort, seed.Sort, value => permission.Sort = value);
        changed |= SeedValues.SetIfChanged(permission.Remark, SeededRemark, value => permission.Remark = value);
        return changed;
    }
}
