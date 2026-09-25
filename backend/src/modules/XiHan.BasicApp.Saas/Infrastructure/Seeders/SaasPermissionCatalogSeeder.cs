// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 权限目录：<see cref="SaasPermissionDefinitions"/> 声明的功能权限
/// </summary>
public sealed class SaasPermissionCatalogSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasPermissionCatalogSeeder> logger,
    IServiceProvider serviceProvider)
    : PermissionCatalogSeederBase(clientResolver, logger, serviceProvider)
{
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
            definition.Sort))];
}
