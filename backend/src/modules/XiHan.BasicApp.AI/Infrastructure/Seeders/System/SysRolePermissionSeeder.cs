// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// 系统角色权限种子数据
/// </summary>
/// <remarks>
/// AI 提供商配置属平台级开发工具：仅授予超级管理员角色，其它任何角色都不得拥有，
/// 故其它租户/角色既看不到也调不动。菜单可见性绑定（ai_provider → ai:read）已前移至 SysMenuSeeder（建菜单即绑定），
/// 本种子只负责角色授权。
/// </remarks>
public class SysRolePermissionSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysRolePermissionSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysRolePermissionSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 204;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]系统角色权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var permissions = await client.Queryable<SysPermission>()
            .Where(p => p.PermissionCode.StartsWith("ai:"))
            .ToListAsync();
        if (permissions.Count == 0)
        {
            Logger.LogWarning("AI 权限不存在，跳过 AI 角色权限种子"); return;
        }

        // AI 提供商配置仅授超级管理员（其它角色一律不得拥有）。超管运行时本就通配 *，此处显式留痕、确保唯一拥有者。
        var superRole = await client.Queryable<SysRole>().FirstAsync(r => r.RoleCode == "super_admin");
        var grantedCount = 0;
        if (superRole is not null)
        {
            var permissionIds = permissions.Select(p => p.BasicId).ToList();
            var existsSet = (await client.Queryable<SysRolePermission>()
                    .Where(rp => rp.RoleId == superRole.BasicId && permissionIds.Contains(rp.PermissionId))
                    .ToListAsync())
                .Select(rp => rp.PermissionId)
                .ToHashSet();
            var addList = permissions
                .Where(p => !existsSet.Contains(p.BasicId))
                .Select(p => new SysRolePermission { RoleId = superRole.BasicId, PermissionId = p.BasicId })
                .ToList();
            if (addList.Count > 0)
            {
                await BulkInsertAsync(addList);
                grantedCount = addList.Count;
            }
        }

        Logger.LogInformation("AI 提供商配置仅授超级管理员：新增角色权限 {GrantCount} 条", grantedCount);
    }
}
