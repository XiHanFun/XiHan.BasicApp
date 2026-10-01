// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 权限目录种子基类：按声明写入一个模块的资源与权限（平台数据，TenantId = 0）
/// </summary>
/// <remarks>
/// 写入口径见 <see cref="PermissionCatalogWriterBase"/>。平台模块一个模块一个种子，在自己的号段里执行。
/// </remarks>
public abstract class PermissionCatalogSeederBase(
    ISqlSugarClientResolver clientResolver,
    ILogger logger,
    IServiceProvider serviceProvider)
    : PermissionCatalogWriterBase(clientResolver, logger, serviceProvider)
{
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
    /// 要写入的权限目录：本模块一份
    /// </summary>
    protected sealed override IEnumerable<PermissionCatalogDeclaration> Catalogs => [new(Name, ModuleCode, Resources, Permissions)];

    /// <summary>
    /// 权限标签：[模块, 分组]
    /// </summary>
    public string BuildTags(PermissionSeed seed)
        => BuildTags(ModuleCode, seed);
}
