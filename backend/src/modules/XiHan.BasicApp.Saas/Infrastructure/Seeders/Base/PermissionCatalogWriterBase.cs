// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 一份权限目录声明：一个模块的资源与权限
/// </summary>
/// <param name="Label">来源名（种子名或登记名，日志与出错信息里标明来源）</param>
/// <param name="ModuleCode">模块编码（权限的模块归属，权限页按模块筛选）</param>
/// <param name="Resources">资源（只有资源型权限需要）</param>
/// <param name="Permissions">权限</param>
public sealed record PermissionCatalogDeclaration(
    string Label,
    string ModuleCode,
    IReadOnlyList<ResourceSeed> Resources,
    IReadOnlyList<PermissionSeed> Permissions);

/// <summary>
/// 权限目录写入基类：按声明写入资源与权限（平台数据，TenantId = 0）
/// </summary>
/// <remarks>
/// 目录由代码定义：已有的资源与权限对齐名称、说明、作用侧、审计、排序等元数据，缺的按声明插入。
/// 启停归运营，只在插入时启用；声明之外的行不删（下线的权限由升级脚本连同引用一起清理）。
/// 资源型权限引用的操作来自平台操作字典，字典里没有就直接报错——说明种子顺序或声明写错了，不能跳过了事。
/// 平台模块经 <see cref="PermissionCatalogSeederBase"/> 一个模块一个种子；业务模块的登记由
/// <see cref="ContributedPermissionCatalogSeeder"/> 汇总写入，两者共用这里的写入口径。
/// </remarks>
public abstract class PermissionCatalogWriterBase(
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
    /// 要写入的权限目录（平台模块一份；业务模块的汇总种子每个登记一份）
    /// </summary>
    protected abstract IEnumerable<PermissionCatalogDeclaration> Catalogs { get; }

    /// <summary>
    /// 种子数据实现
    /// </summary>
    /// <remarks>
    /// 同一个资源码或权限码出现在多份声明里直接报错：谁覆盖谁取决于写入顺序，不能静默。
    /// </remarks>
    protected override async Task SeedInternalAsync()
    {
        var catalogs = Catalogs.ToList();
        ThrowIfDuplicated("资源码", catalogs.SelectMany(catalog => catalog.Resources.Select(resource => (catalog.Label, resource.Code))));
        ThrowIfDuplicated("权限码", catalogs.SelectMany(catalog => catalog.Permissions.Select(permission => (catalog.Label, permission.Code))));

        foreach (var catalog in catalogs)
        {
            await SeedCatalogAsync(catalog.Label, catalog.ModuleCode, catalog.Resources, catalog.Permissions);
        }
    }

    /// <summary>
    /// 权限标签：[模块, 分组]
    /// </summary>
    /// <param name="moduleCode">模块编码</param>
    /// <param name="seed">权限声明</param>
    public static string BuildTags(string moduleCode, PermissionSeed seed)
    {
        return JsonSerializer.Serialize(new[] { moduleCode, seed.Group });
    }

    /// <summary>
    /// 写入一个模块的资源与权限
    /// </summary>
    /// <param name="label">出错与日志里的来源名（种子名或登记名）</param>
    /// <param name="moduleCode">模块编码（权限的模块归属，权限页按模块筛选）</param>
    /// <param name="resources">资源（只有资源型权限需要）</param>
    /// <param name="permissions">权限</param>
    private async Task SeedCatalogAsync(string label, string moduleCode, IReadOnlyList<ResourceSeed> resources, IReadOnlyList<PermissionSeed> permissions)
    {
        // 资源与操作成对出现：只给一个会落成类型是资源型、却缺另一半的权限，管理端的创建校验也不认这种形态
        var halfBound = permissions
            .Where(static permission => permission.Resource is null != permission.Operation is null)
            .Select(static permission => permission.Code)
            .ToList();
        if (halfBound.Count > 0)
        {
            throw new InvalidOperationException($"{label}：{string.Join("、", halfBound)} 的资源与操作须同时声明或同时留空。");
        }

        var resourceIds = await SeedResourcesAsync(label, resources);
        var operationIds = await LoadOperationIdsAsync(label, permissions);
        await SeedPermissionsAsync(label, moduleCode, permissions, resourceIds, operationIds);
    }

    private void ThrowIfDuplicated(string kind, IEnumerable<(string Source, string Code)> entries)
    {
        var duplicated = entries
            .GroupBy(entry => entry.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}（{string.Join("、", group.Select(entry => entry.Source))}）")
            .ToList();
        if (duplicated.Count > 0)
        {
            throw new InvalidOperationException($"{Name}：以下{kind}被多处登记：{string.Join("；", duplicated)}。");
        }
    }

    private async Task<Dictionary<string, long>> SeedResourcesAsync(string label, IReadOnlyList<ResourceSeed> resources)
    {
        var codes = resources.Select(static resource => resource.Code).ToList();
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
        foreach (var seed in resources)
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
            Logger.LogInformation("{Seeder}：资源新增 {AddCount} 个、对齐 {UpdateCount} 个", label, adding.Count, updated);
        }

        return (await DbClient.Queryable<SysResource>()
                .Where(resource => resource.TenantId == 0 && codes.Contains(resource.ResourceCode))
                .ToListAsync())
            .ToDictionary(resource => resource.ResourceCode, resource => resource.BasicId, StringComparer.Ordinal);
    }

    private async Task<Dictionary<string, long>> LoadOperationIdsAsync(string label, IReadOnlyList<PermissionSeed> permissions)
    {
        var codes = permissions
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
            : throw new InvalidOperationException($"{label}：操作字典里没有 {string.Join("、", missing)}，操作字典种子须先于权限目录执行。");
    }

    private async Task SeedPermissionsAsync(
        string label,
        string moduleCode,
        IReadOnlyList<PermissionSeed> permissions,
        IReadOnlyDictionary<string, long> resourceIds,
        IReadOnlyDictionary<string, long> operationIds)
    {
        var codes = permissions.Select(static permission => permission.Code).ToList();
        var existing = (await DbClient.Queryable<SysPermission>()
                .Where(permission => permission.TenantId == 0 && codes.Contains(permission.PermissionCode))
                .ToListAsync())
            .ToDictionary(permission => permission.PermissionCode, StringComparer.Ordinal);

        var adding = new List<SysPermission>();
        var updated = 0;
        foreach (var seed in permissions)
        {
            var resourceId = seed.Resource is null ? (long?)null : resourceIds[seed.Resource.Code];
            var operationId = seed.Operation is null ? (long?)null : operationIds[seed.Operation.Code];
            if (existing.TryGetValue(seed.Code, out var permission))
            {
                if (ApplyPermission(permission, moduleCode, seed, resourceId, operationId))
                {
                    _ = await DbClient.Updateable(permission).ExecuteCommandAsync();
                    updated++;
                }

                continue;
            }

            permission = new SysPermission { PermissionCode = seed.Code, Status = EnableStatus.Enabled };
            _ = ApplyPermission(permission, moduleCode, seed, resourceId, operationId);
            adding.Add(permission);
        }

        if (adding.Count > 0)
        {
            await BulkInsertAsync(adding);
        }

        Logger.LogInformation("{Seeder}：权限新增 {AddCount} 个、对齐 {UpdateCount} 个", label, adding.Count, updated);
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

    private static bool ApplyPermission(SysPermission permission, string moduleCode, PermissionSeed seed, long? resourceId, long? operationId)
    {
        var changed = false;
        changed |= SeedValues.SetIfChanged(permission.PermissionType, seed.Resource is null ? PermissionType.Functional : PermissionType.ResourceBased, value => permission.PermissionType = value);
        changed |= SeedValues.SetIfChanged(permission.ResourceId, resourceId, value => permission.ResourceId = value);
        changed |= SeedValues.SetIfChanged(permission.OperationId, operationId, value => permission.OperationId = value);
        changed |= SeedValues.SetIfChanged(permission.ModuleCode, moduleCode, value => permission.ModuleCode = value);
        changed |= SeedValues.SetIfChanged(permission.PermissionName, seed.Name, value => permission.PermissionName = value);
        changed |= SeedValues.SetIfChanged(permission.PermissionDescription, seed.Description, value => permission.PermissionDescription = value);
        changed |= SeedValues.SetIfChanged(permission.Tags, BuildTags(moduleCode, seed), value => permission.Tags = value);
        changed |= SeedValues.SetIfChanged(permission.Side, seed.Side, value => permission.Side = value);
        changed |= SeedValues.SetIfChanged(permission.IsRequireAudit, seed.IsRequireAudit, value => permission.IsRequireAudit = value);
        changed |= SeedValues.SetIfChanged(permission.Priority, seed.Sort, value => permission.Priority = value);
        changed |= SeedValues.SetIfChanged(permission.Sort, seed.Sort, value => permission.Sort = value);
        changed |= SeedValues.SetIfChanged(permission.Remark, SeededRemark, value => permission.Remark = value);
        return changed;
    }
}
