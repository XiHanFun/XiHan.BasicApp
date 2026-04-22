#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRolePermissionSeeder
// Guid:5e6f7890-1234-5678-ef01-445566778899
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 12:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统角色权限关系种子数据。
/// </summary>
public class SysRolePermissionSeeder : DataSeederBase
{
    public SysRolePermissionSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysRolePermissionSeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.RolePermissions;

    public override string Name => "[Saas]系统角色权限关系种子数据";

    protected override async Task SeedInternalAsync()
    {
        var roles = await DbClient
            .Queryable<SysRole>()
            .Where(role => role.TenantId == SaasSeedDefaults.PlatformTenantId && role.IsGlobal)
            .ToListAsync();
        var permissions = await DbClient
            .Queryable<SysPermission>()
            .Where(permission => permission.TenantId == SaasSeedDefaults.PlatformTenantId && permission.IsGlobal && permission.Status == YesOrNo.Yes)
            .ToListAsync();

        if (roles.Count == 0 || permissions.Count == 0)
        {
            Logger.LogWarning("角色或权限模板不存在，跳过角色权限关系种子");
            return;
        }

        var roleMap = roles.ToDictionary(role => role.RoleCode, StringComparer.OrdinalIgnoreCase);
        var permissionMap = permissions.ToDictionary(permission => permission.PermissionCode, StringComparer.OrdinalIgnoreCase);
        var requiredPairs = new HashSet<(long RoleId, long PermissionId)>();

        AddPairs("super_admin", permissionCode => true);
        AddPairs("platform_admin", permissionCode =>
            !permissionCode.StartsWith("tenant:delete", StringComparison.OrdinalIgnoreCase)
            && !permissionCode.StartsWith("super_admin:", StringComparison.OrdinalIgnoreCase));
        AddPairs("tenant_owner", permissionCode =>
            !permissionCode.StartsWith("tenant:", StringComparison.OrdinalIgnoreCase)
            && !permissionCode.StartsWith("permission:", StringComparison.OrdinalIgnoreCase)
            && !permissionCode.StartsWith("constraint_rule:", StringComparison.OrdinalIgnoreCase)
            && !permissionCode.StartsWith("field_level_security:", StringComparison.OrdinalIgnoreCase)
            && !permissionCode.EndsWith(":revoke", StringComparison.OrdinalIgnoreCase));
        AddPairs("tenant_admin", permissionCode =>
            (permissionCode.StartsWith("user:", StringComparison.OrdinalIgnoreCase)
             || permissionCode.StartsWith("role:", StringComparison.OrdinalIgnoreCase)
             || permissionCode.StartsWith("department:", StringComparison.OrdinalIgnoreCase)
             || permissionCode.StartsWith("message:", StringComparison.OrdinalIgnoreCase)
             || permissionCode.StartsWith("notification:", StringComparison.OrdinalIgnoreCase)
             || permissionCode.StartsWith("review:", StringComparison.OrdinalIgnoreCase))
            && !permissionCode.EndsWith(":delete", StringComparison.OrdinalIgnoreCase)
            && !permissionCode.EndsWith(":revoke", StringComparison.OrdinalIgnoreCase));
        AddPairs("tenant_member", permissionCode =>
            permissionCode is
                "message:read" or "message:view" or
                "notification:read" or "notification:view" or
                "review:read" or "review:view" or
                "file:read" or "file:view" or "file:download");
        AddPairs("tenant_viewer", permissionCode =>
            permissionCode.EndsWith(":read", StringComparison.OrdinalIgnoreCase)
            || permissionCode.EndsWith(":view", StringComparison.OrdinalIgnoreCase));
        AddPairs("external_collaborator", permissionCode =>
            permissionCode is
                "message:read" or "message:view" or
                "review:read" or "review:view" or
                "file:read" or "file:view" or "file:download");

        var roleIds = requiredPairs.Select(item => item.RoleId).Distinct().ToArray();
        var permissionIds = requiredPairs.Select(item => item.PermissionId).Distinct().ToArray();
        var existingPairs = await DbClient
            .Queryable<SysRolePermission>()
            .Where(item => roleIds.Contains(item.RoleId) && permissionIds.Contains(item.PermissionId))
            .Select(item => new { item.BasicId, item.RoleId, item.PermissionId, item.Status })
            .ToListAsync();

        var existingSet = existingPairs
            .Select(item => $"{item.RoleId}_{item.PermissionId}")
            .ToHashSet(StringComparer.Ordinal);
        var toInsert = requiredPairs
            .Where(item => !existingSet.Contains($"{item.RoleId}_{item.PermissionId}"))
            .Select(item => new SysRolePermission
            {
                RoleId = item.RoleId,
                PermissionId = item.PermissionId,
                Status = YesOrNo.Yes
            })
            .ToList();

        if (toInsert.Count > 0)
        {
            await BulkInsertAsync(toInsert);
        }

        var toEnableIds = existingPairs
            .Where(item => item.Status != YesOrNo.Yes)
            .Select(item => item.BasicId)
            .ToArray();

        if (toEnableIds.Length > 0)
        {
            await DbClient
                .Updateable<SysRolePermission>()
                .SetColumns(item => item.Status == YesOrNo.Yes)
                .Where(item => toEnableIds.Contains(item.BasicId))
                .ExecuteCommandAsync();
        }

        Logger.LogInformation(
            "系统角色权限模板种子完成：新增 {InsertCount} 项，启用 {EnableCount} 项",
            toInsert.Count,
            toEnableIds.Length);

        void AddPairs(string roleCode, Func<string, bool> predicate)
        {
            if (!roleMap.TryGetValue(roleCode, out var role))
            {
                return;
            }

            foreach (var permission in permissions)
            {
                if (predicate(permission.PermissionCode) && permissionMap.ContainsKey(permission.PermissionCode))
                {
                    requiredPairs.Add((role.BasicId, permission.BasicId));
                }
            }
        }
    }
}
