// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
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

    /// <summary>
    /// 把权限目录里这些权限码的作用侧同步为定义值
    /// </summary>
    /// <remarks>
    /// 只插入不更新的权限种子也要保证已落库的行与定义的作用侧一致：作用侧决定权限在哪个上下文生效、
    /// 能否进入套餐白名单，旧库里未声明（0）或声明过时的行不能留着。
    /// </remarks>
    /// <param name="permissionCodes">权限码</param>
    /// <param name="side">定义的作用侧</param>
    protected async Task SyncPermissionSideAsync(IReadOnlyCollection<string> permissionCodes, PermissionSide side)
    {
        if (permissionCodes.Count == 0)
        {
            return;
        }

        var codes = permissionCodes.ToList();
        var synced = await DbClient.Updateable<SysPermission>()
            .SetColumns(permission => permission.Side == side)
            .Where(permission => permission.TenantId == 0 && codes.Contains(permission.PermissionCode) && permission.Side != side)
            .ExecuteCommandAsync();
        if (synced > 0)
        {
            Logger.LogInformation("同步 {Count} 个权限的作用侧为 {Side}", synced, side);
        }
    }
}
