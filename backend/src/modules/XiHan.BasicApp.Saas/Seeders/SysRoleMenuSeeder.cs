#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleMenuSeeder
// Guid:4a7f1902-5376-4c2b-9f89-c286fe03ff31
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/09 09:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统角色菜单关系种子数据
/// </summary>
public class SysRoleMenuSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysRoleMenuSeeder(ISqlSugarDbContext dbContext, ILogger<SysRoleMenuSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 17;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Rbac]系统角色菜单关系种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        if (await HasDataAsync<SysRoleMenu>(r => true))
        {
            Logger.LogInformation("系统角色菜单关系数据已存在，跳过种子数据");
            return;
        }

        var roles = await DbContext.GetClient().Queryable<SysRole>().ToListAsync();
        var menus = await DbContext.GetClient().Queryable<SysMenu>().ToListAsync();

        if (roles.Count == 0 || menus.Count == 0)
        {
            Logger.LogWarning("角色或菜单数据不存在，跳过角色菜单关系种子数据");
            return;
        }

        var roleMap = roles
            .Where(role => !string.IsNullOrWhiteSpace(role.RoleCode))
            .GroupBy(role => role.RoleCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(role => role.TenantId.HasValue ? 1 : 0)
                    .ThenBy(role => role.BasicId)
                    .First()
                    .BasicId,
                StringComparer.OrdinalIgnoreCase);

        var menuMap = menus
            .Where(menu => !string.IsNullOrWhiteSpace(menu.MenuCode))
            .GroupBy(menu => menu.MenuCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(menu => menu.TenantId.HasValue ? 1 : 0)
                    .ThenBy(menu => menu.BasicId)
                    .First()
                    .BasicId,
                StringComparer.OrdinalIgnoreCase);
        var allMenuCodes = menuMap.Keys.ToArray();

        var roleMenuCodeMap = new Dictionary<string, string[]>
        {
            // 超级管理员拥有全部菜单
            ["super_admin"] = allMenuCodes,

            // 系统管理员保留后台核心管理菜单
            ["admin"] =
            [
                "dashboard",
                "system", "user", "role", "department", "permission", "notice", "oauth_app", "user_session", "review",
                "platform", "menu", "config", "dict", "task", "monitor", "cache", "file",
                "messaging", "message", "email", "sms",
                "log", "access_log", "operation_log", "exception_log", "audit_log",
                "about"
            ],

            // 其余角色按常用场景给出基础菜单，便于开箱联调
            ["dept_admin"] = ["dashboard", "system", "user", "role", "department", "permission", "notice", "user_session", "review", "messaging", "message", "about"],
            ["dept_manager"] = ["dashboard", "system", "user", "department", "notice", "messaging", "message", "about"],
            ["employee"] = ["dashboard", "notice", "about"],
            ["guest"] = ["dashboard"]
        };

        var requiredPairs = new List<(long RoleId, long MenuId)>();
        foreach (var (roleCode, menuCodes) in roleMenuCodeMap)
        {
            if (!roleMap.TryGetValue(roleCode, out var roleId))
            {
                continue;
            }

            foreach (var menuCode in menuCodes.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (menuMap.TryGetValue(menuCode, out var menuId))
                {
                    requiredPairs.Add((roleId, menuId));
                }
            }
        }

        if (requiredPairs.Count == 0)
        {
            Logger.LogWarning("未解析到任何角色菜单关系，跳过角色菜单关系种子数据");
            return;
        }

        var targetRoleIds = requiredPairs.Select(pair => pair.RoleId).Distinct().ToArray();
        var targetMenuIds = requiredPairs.Select(pair => pair.MenuId).Distinct().ToArray();

        var existingPairs = await DbContext.GetClient()
            .Queryable<SysRoleMenu>()
            .Where(mapping => targetRoleIds.Contains(mapping.RoleId) && targetMenuIds.Contains(mapping.MenuId))
            .Select(mapping => new { mapping.RoleId, mapping.MenuId })
            .ToListAsync();

        var existingSet = existingPairs
            .Select(pair => $"{pair.RoleId}_{pair.MenuId}")
            .ToHashSet(StringComparer.Ordinal);

        var roleMenus = requiredPairs
            .Where(pair => !existingSet.Contains($"{pair.RoleId}_{pair.MenuId}"))
            .Select(pair => new SysRoleMenu
            {
                RoleId = pair.RoleId,
                MenuId = pair.MenuId,
                Status = YesOrNo.Yes
            })
            .ToList();

        if (roleMenus.Count == 0)
        {
            Logger.LogInformation("角色菜单关系数据已存在，跳过新增");
            return;
        }

        await BulkInsertAsync(roleMenus);
        Logger.LogInformation("成功初始化 {Count} 个角色菜单关系", roleMenus.Count);
    }
}
