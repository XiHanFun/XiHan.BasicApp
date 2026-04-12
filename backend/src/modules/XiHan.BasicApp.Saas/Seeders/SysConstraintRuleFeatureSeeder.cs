#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConstraintRuleFeatureSeeder
// Guid:6bc376af-f098-4f0d-b043-0efb4f7af6f4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 约束规则功能增量种子数据（兼容已有数据库）
/// </summary>
public class SysConstraintRuleFeatureSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysConstraintRuleFeatureSeeder(ISqlSugarDbContext dbContext, ILogger<SysConstraintRuleFeatureSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 21;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Saas]约束规则功能增量种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var db = DbContext.GetClient();

        var constraintResource = await db.Queryable<SysResource>()
            .FirstAsync(resource => resource.ResourceCode == "constraint_rule");

        if (constraintResource is null)
        {
            constraintResource = new SysResource
            {
                ResourceCode = "constraint_rule",
                ResourceName = "约束规则",
                ResourceType = ResourceType.Api,
                ResourcePath = "/api/constraint-rules",
                Description = "约束规则管理API接口",
                IsRequireAuth = true,
                IsPublic = false,
                Status = YesOrNo.Yes,
                Sort = 2031
            };
            await db.Insertable(constraintResource).ExecuteReturnEntityAsync();
        }

        var platformMenu = await db.Queryable<SysMenu>()
            .FirstAsync(menu => menu.MenuCode == "platform");
        var constraintMenu = await db.Queryable<SysMenu>()
            .FirstAsync(menu => menu.MenuCode == "constraint_rule");

        if (constraintMenu is null)
        {
            constraintMenu = new SysMenu
            {
                PermissionCode = "constraint_rule:read",
                ParentId = platformMenu?.BasicId,
                MenuName = "约束规则",
                MenuCode = "constraint_rule",
                MenuType = MenuType.Menu,
                Path = "/platform/constraint-rule",
                Component = "System/ConstraintRule/Index",
                RouteName = "SystemConstraintRule",
                Icon = "scale",
                Title = "约束规则",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 2041
            };
            await db.Insertable(constraintMenu).ExecuteReturnEntityAsync();
        }

        var requiredOperationCodes = new[] { "read", "create", "update", "delete", "import", "export" };
        var operations = await db.Queryable<SysOperation>()
            .Where(operation => requiredOperationCodes.Contains(operation.OperationCode))
            .ToListAsync();

        var existingPermissionCodes = await db.Queryable<SysPermission>()
            .Where(permission => permission.PermissionCode.StartsWith("constraint_rule:"))
            .Select(permission => permission.PermissionCode)
            .ToListAsync();

        var permissionInserts = new List<SysPermission>();
        foreach (var operationCode in requiredOperationCodes)
        {
            var permissionCode = $"constraint_rule:{operationCode}";
            if (existingPermissionCodes.Contains(permissionCode, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var operation = operations.FirstOrDefault(item =>
                string.Equals(item.OperationCode, operationCode, StringComparison.OrdinalIgnoreCase));
            if (operation is null)
            {
                continue;
            }

            permissionInserts.Add(new SysPermission
            {
                ResourceId = constraintResource.BasicId,
                OperationId = operation.BasicId,
                PermissionCode = permissionCode,
                PermissionName = $"约束规则-{operation.OperationName}",
                PermissionDescription = $"对约束规则执行{operation.OperationName}操作",
                IsRequireAudit = operation.IsRequireAudit,
                Tags = "admin",
                Status = YesOrNo.Yes,
                Sort = 9000 + permissionInserts.Count
            });
        }

        if (permissionInserts.Count > 0)
        {
            await BulkInsertAsync(permissionInserts);
        }

        var constraintPermissions = await db.Queryable<SysPermission>()
            .Where(permission => permission.PermissionCode.StartsWith("constraint_rule:"))
            .ToListAsync();
        if (constraintPermissions.Count == 0)
        {
            Logger.LogWarning("未找到约束规则权限，跳过角色授权映射");
            return;
        }

        var roles = await db.Queryable<SysRole>()
            .Where(role => role.RoleCode == "super_admin" || role.RoleCode == "admin")
            .ToListAsync();
        if (roles.Count == 0)
        {
            Logger.LogWarning("未找到 super_admin/admin 角色，跳过约束规则角色授权映射");
            return;
        }

        var requiredRolePermissionPairs = new List<(long RoleId, long PermissionId)>();
        foreach (var role in roles)
        {
            var isAdmin = string.Equals(role.RoleCode, "admin", StringComparison.OrdinalIgnoreCase);
            foreach (var permission in constraintPermissions)
            {
                if (isAdmin && permission.PermissionCode.EndsWith(":delete", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                requiredRolePermissionPairs.Add((role.BasicId, permission.BasicId));
            }
        }

        var roleIds = requiredRolePermissionPairs.Select(pair => pair.RoleId).Distinct().ToArray();
        var permissionIds = requiredRolePermissionPairs.Select(pair => pair.PermissionId).Distinct().ToArray();
        var existingRolePermissions = await db.Queryable<SysRolePermission>()
            .Where(mapping => roleIds.Contains(mapping.RoleId) && permissionIds.Contains(mapping.PermissionId))
            .Select(mapping => new { mapping.RoleId, mapping.PermissionId })
            .ToListAsync();
        var existingRolePermissionSet = existingRolePermissions
            .Select(item => $"{item.RoleId}_{item.PermissionId}")
            .ToHashSet(StringComparer.Ordinal);

        var rolePermissionInserts = requiredRolePermissionPairs
            .Where(pair => !existingRolePermissionSet.Contains($"{pair.RoleId}_{pair.PermissionId}"))
            .Select(pair => new SysRolePermission
            {
                RoleId = pair.RoleId,
                PermissionId = pair.PermissionId
            })
            .ToList();
        if (rolePermissionInserts.Count > 0)
        {
            await BulkInsertAsync(rolePermissionInserts);
        }

        if (constraintMenu is null)
        {
            return;
        }

        var existingRoleMenus = await db.Queryable<SysRoleMenu>()
            .Where(mapping => roleIds.Contains(mapping.RoleId) && mapping.MenuId == constraintMenu.BasicId)
            .Select(mapping => mapping.RoleId)
            .ToListAsync();
        var existingRoleMenuSet = existingRoleMenus.ToHashSet();

        var roleMenuInserts = roleIds
            .Where(roleId => !existingRoleMenuSet.Contains(roleId))
            .Select(roleId => new SysRoleMenu
            {
                RoleId = roleId,
                MenuId = constraintMenu.BasicId
            })
            .ToList();
        if (roleMenuInserts.Count > 0)
        {
            await BulkInsertAsync(roleMenuInserts);
        }

        Logger.LogInformation(
            "约束规则增量种子完成：新增权限 {PermissionCount} 条，新增角色权限 {RolePermissionCount} 条，新增角色菜单 {RoleMenuCount} 条",
            permissionInserts.Count,
            rolePermissionInserts.Count,
            roleMenuInserts.Count);
    }
}
