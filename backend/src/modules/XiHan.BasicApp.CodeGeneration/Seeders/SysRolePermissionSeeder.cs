#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRolePermissionSeeder
// Guid:46e7af60-ef56-4326-ac87-cc76f8891ace
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 13:13:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.CodeGeneration.Seeders;

/// <summary>
/// 系统角色权限种子数据
/// </summary>
/// <remarks>
/// 开发功能（代码生成）属平台级开发工具：仅授予超级管理员角色，其它任何角色都不得拥有；
/// 并把"代码生成"菜单绑定到 code_gen:read 权限点，使其仅对拥有该权限者（=超管）可见，
/// develop 目录因无可见子菜单被自动剪掉。其它租户/角色既看不到也调不动开发功能。
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
    public override int Order => 33;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[CodeGeneration]系统角色权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var permissions = await client.Queryable<SysPermission>()
            .Where(p => p.PermissionCode.StartsWith("code_gen:") || p.PermissionCode.StartsWith("code_gen_api:"))
            .ToListAsync();
        if (permissions.Count == 0)
        {
            Logger.LogWarning("代码生成权限不存在，跳过开发功能角色权限/菜单可见性种子"); return;
        }

        // 1) 开发功能仅授超级管理员（其它角色一律不得拥有）。超管运行时本就通配 *，此处显式留痕、确保唯一拥有者。
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

        // 2) 把"开发工具"目录(develop)与"代码生成"菜单(code_gen)都绑定到 code_gen:read 权限
        //    → 仅有该权限者（=超管 *）可见；非超管两者皆隐藏（目录直接受控，不依赖"空目录剪枝"）。
        // 菜单种子(Order=31)早于权限种子(Order=32)，故菜单建立时无法绑定，统一在此(Order=33)回填。
        // 注意：Where 不可用 (PermissionId == null || PermissionId != x) 这种"可空列 OR"——SqlSugar 在
        // PostgreSQL 上会错译(IS NULL + OR 丢失/语法错误)，导致更新 0 行、绑定不生效。改用 MenuCode IN(...)
        // 无条件回填（幂等：重复设同值无副作用）。
        var readPermission = permissions.FirstOrDefault(p => p.PermissionCode == "code_gen:read");
        var menuBound = 0;
        if (readPermission is not null)
        {
            var devMenuCodes = new[] { "develop", "code_gen" };
            menuBound = await client.Updateable<SysMenu>()
                .SetColumns(m => m.PermissionId == readPermission.BasicId)
                .Where(m => devMenuCodes.Contains(m.MenuCode))
                .ExecuteCommandAsync();
        }

        Logger.LogInformation("开发功能仅授超级管理员：新增角色权限 {GrantCount} 条，菜单可见性绑定 {MenuBound} 条", grantedCount, menuBound);
    }
}
