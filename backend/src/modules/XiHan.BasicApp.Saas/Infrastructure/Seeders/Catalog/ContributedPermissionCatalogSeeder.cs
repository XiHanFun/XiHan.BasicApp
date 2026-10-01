// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 业务模块权限目录的汇总种子：在权限目录阶段最后写入全部 <see cref="IPermissionCatalogContribution"/>
/// </summary>
/// <remarks>
/// 业务模块（含代码生成产物）不各自占种子顺序号，也不需要登记种子：登记类按约定注册即被这里收齐。
/// 不同登记里出现相同的资源码或权限码直接报错（见 <see cref="PermissionCatalogWriterBase"/>）。
/// </remarks>
public sealed class ContributedPermissionCatalogSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<ContributedPermissionCatalogSeeder> logger,
    IServiceProvider serviceProvider,
    IEnumerable<IPermissionCatalogContribution> contributions)
    : PermissionCatalogWriterBase(clientResolver, logger, serviceProvider)
{
    private readonly IReadOnlyList<IPermissionCatalogContribution> _contributions =
        [.. contributions.OrderBy(contribution => contribution.Name, StringComparer.Ordinal)];

    /// <summary>
    /// 种子数据优先级：权限目录阶段的业务号段，排在全部平台模块之后
    /// </summary>
    public override int Order => SeedOrders.PermissionCatalog + SeedOrders.BusinessBand;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]业务模块权限目录";

    /// <summary>
    /// 要写入的权限目录：每个登记一份
    /// </summary>
    protected override IEnumerable<PermissionCatalogDeclaration> Catalogs => _contributions
        .Select(contribution => new PermissionCatalogDeclaration(
            $"{Name}·{contribution.Name}",
            contribution.ModuleCode,
            contribution.Resources,
            contribution.Permissions));
}
