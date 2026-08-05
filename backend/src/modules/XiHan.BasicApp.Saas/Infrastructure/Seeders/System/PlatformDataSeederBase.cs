// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// 平台级种子数据基类：整个播种过程在平台租户上下文（TenantId = 0）内执行。
/// </summary>
/// <remarks>
/// 操作字典、资源、权限、角色权限这类记录属于平台级，按 <c>BasicAppEntity</c> 的约定必须落在
/// TenantId = 0。它们的 TenantId 由写入拦截器按当时的租户上下文注入，而应用启动播种时上下文
/// 未必是平台租户，因此必须在此显式切换。
/// <para>
/// 不切换的后果是静默的：行照常写入，但落在了别的租户下，凡是按 TenantId = 0 查找的消费方
/// （如 <see cref="PageRegistryMenuSeederBase"/> 解析菜单依赖的权限）都会查不到并跳过，
/// 表现为「菜单莫名其妙少了几个」。
/// </para>
/// </remarks>
public abstract class PlatformDataSeederBase : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    protected PlatformDataSeederBase(ISqlSugarClientResolver clientResolver, ILogger logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 在平台租户上下文内执行播种。
    /// </summary>
    public override async Task SeedAsync()
    {
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();
        using var platformScope = currentTenant.Change(null);
        await base.SeedAsync();
    }
}
