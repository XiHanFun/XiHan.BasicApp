// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 权限目录：<see cref="SaasPermissionDefinitions"/> 声明的资源型权限
/// </summary>
/// <remarks>
/// 权限码是「saas:分组:动作」：每个分组落成一个资源（资源编码即分组码），动作取操作字典里的同名操作。
/// 名称、说明、审计与排序沿用权限定义本身，不随操作改写。
/// </remarks>
public sealed class SaasPermissionCatalogSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasPermissionCatalogSeeder> logger,
    IServiceProvider serviceProvider)
    : PermissionCatalogSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 分组即资源：路径留空（一个分组横跨多个接口服务），排序随分组声明顺序
    /// </summary>
    private static readonly IReadOnlyList<ResourceSeed> GroupResources = [.. SaasPermissionDefinitions.Groups
        .Select(static (group, index) => new ResourceSeed(group.GroupCode, group.GroupName, null, $"{group.GroupName}相关接口", index + 1))];

    private static readonly Dictionary<string, ResourceSeed> ResourceByGroup = GroupResources
        .ToDictionary(static resource => resource.Code, StringComparer.Ordinal);

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PermissionCatalog;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]权限目录";

    /// <summary>
    /// 模块编码
    /// </summary>
    public override string ModuleCode => SaasPermissionCodes.Module;

    /// <summary>
    /// 本模块的资源
    /// </summary>
    public override IReadOnlyList<ResourceSeed> Resources => GroupResources;

    /// <summary>
    /// 本模块的权限
    /// </summary>
    public override IReadOnlyList<PermissionSeed> Permissions { get; } = [.. SaasPermissionDefinitions.All
        .Select(static definition => new PermissionSeed(
            definition.PermissionCode,
            definition.PermissionName,
            definition.PermissionDescription,
            definition.GroupCode,
            definition.Side,
            definition.IsRequireAudit,
            definition.Sort,
            ResourceByGroup[definition.GroupCode],
            OperationSeeds.Get(definition.PermissionCode[(definition.PermissionCode.LastIndexOf(':') + 1)..])))];
}
