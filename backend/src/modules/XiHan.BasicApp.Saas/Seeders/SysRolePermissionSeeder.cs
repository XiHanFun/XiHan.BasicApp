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
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统角色权限关系种子数据
/// </summary>
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
    public override int Order => 13;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => $"[Saas]系统角色权限关系种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var roles = await DbClient.Queryable<SysRole>().ToListAsync();
        var permissions = await DbClient
            .Queryable<SysPermission>()
            .Where(permission => permission.Status == YesOrNo.Yes)
            .ToListAsync();

        if (roles.Count == 0 || permissions.Count == 0)
        {
            Logger.LogWarning("角色或权限数据不存在，跳过角色权限关系种子数据");
            return;
        }

        var roleMap = roles
            .Where(role => !string.IsNullOrWhiteSpace(role.RoleCode))
            .GroupBy(role => role.RoleCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(role => role.TenantId != 0 ? 1 : 0)
                    .ThenBy(role => role.BasicId)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var requiredPairs = new HashSet<(long RoleId, long PermissionId)>();

        if (roleMap.TryGetValue("super_admin", out var superAdminRoles))
        {
            foreach (var superAdminRole in superAdminRoles)
            {
                foreach (var permission in FilterPermissionsByTenantScope(superAdminRole.TenantId))
                {
                    requiredPairs.Add((superAdminRole.BasicId, permission.BasicId));
                }
            }
        }

        AddRolePermissionPairs("admin", code =>
            !code.Contains(":delete", StringComparison.OrdinalIgnoreCase) &&
            !code.Contains(":revoke", StringComparison.OrdinalIgnoreCase) &&
            !code.Contains("tenant:", StringComparison.OrdinalIgnoreCase));

        AddRolePermissionPairs("dept_admin", code =>
            code.StartsWith("user:", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("department:", StringComparison.OrdinalIgnoreCase) ||
            (code.StartsWith("role:", StringComparison.OrdinalIgnoreCase)
                && !code.Contains(":delete", StringComparison.OrdinalIgnoreCase)));

        AddRolePermissionPairs("dept_manager", code =>
            code.Contains(":read", StringComparison.OrdinalIgnoreCase) ||
            code.Contains(":view", StringComparison.OrdinalIgnoreCase) ||
            (code.StartsWith("user:", StringComparison.OrdinalIgnoreCase)
                && (code.Contains(":update", StringComparison.OrdinalIgnoreCase)
                    || code.Contains(":create", StringComparison.OrdinalIgnoreCase))));

        AddRolePermissionPairs("employee", code =>
            code.Contains(":read", StringComparison.OrdinalIgnoreCase)
            || code.Contains(":view", StringComparison.OrdinalIgnoreCase));

        AddRolePermissionPairs("guest", code =>
            string.Equals(code, "user:read", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "department:read", StringComparison.OrdinalIgnoreCase));

        if (requiredPairs.Count == 0)
        {
            Logger.LogWarning("未解析到任何角色权限关系，跳过角色权限关系种子数据");
            return;
        }

        var targetRoleIds = requiredPairs.Select(pair => pair.RoleId).Distinct().ToArray();
        var targetPermissionIds = requiredPairs.Select(pair => pair.PermissionId).Distinct().ToArray();
        var existingPairs = await DbClient
            .Queryable<SysRolePermission>()
            .Where(mapping => targetRoleIds.Contains(mapping.RoleId) && targetPermissionIds.Contains(mapping.PermissionId))
            .Select(mapping => new { mapping.BasicId, mapping.RoleId, mapping.PermissionId, mapping.Status })
            .ToListAsync();

        var existingSet = existingPairs
            .Select(pair => $"{pair.RoleId}_{pair.PermissionId}")
            .ToHashSet(StringComparer.Ordinal);

        var rolePermissions = requiredPairs
            .Where(pair => !existingSet.Contains($"{pair.RoleId}_{pair.PermissionId}"))
            .Select(pair => new SysRolePermission
            {
                RoleId = pair.RoleId,
                PermissionId = pair.PermissionId
            })
            .ToList();

        if (rolePermissions.Count > 0)
        {
            await BulkInsertAsync(rolePermissions);
        }

        var disabledMappingIds = existingPairs
            .Where(pair =>
                pair.Status != YesOrNo.Yes
                && requiredPairs.Contains((pair.RoleId, pair.PermissionId)))
            .Select(pair => pair.BasicId)
            .Distinct()
            .ToArray();
        if (disabledMappingIds.Length > 0)
        {
            await DbClient
                .Updateable<SysRolePermission>()
                .SetColumns(mapping => mapping.Status == YesOrNo.Yes)
                .Where(mapping => disabledMappingIds.Contains(mapping.BasicId))
                .ExecuteCommandAsync();
        }

        if (rolePermissions.Count == 0 && disabledMappingIds.Length == 0)
        {
            Logger.LogInformation("角色权限关系数据已存在，跳过新增");
            return;
        }

        Logger.LogInformation(
            "角色权限关系种子完成：新增 {InsertCount} 条，启用 {EnableCount} 条",
            rolePermissions.Count,
            disabledMappingIds.Length);

        void AddRolePermissionPairs(string roleCode, Func<string, bool> permissionPredicate)
        {
            if (!roleMap.TryGetValue(roleCode, out var roleEntries))
            {
                return;
            }

            foreach (var roleEntry in roleEntries)
            {
                foreach (var permission in FilterPermissionsByTenantScope(roleEntry.TenantId))
                {
                    if (!string.IsNullOrWhiteSpace(permission.PermissionCode)
                        && permissionPredicate(permission.PermissionCode))
                    {
                        requiredPairs.Add((roleEntry.BasicId, permission.BasicId));
                    }
                }
            }
        }

        List<SysPermission> FilterPermissionsByTenantScope(long? roleTenantId)
        {
            if (roleTenantId.HasValue)
            {
                return [.. permissions.Where(permission => permission.TenantId == 0 || permission.TenantId == roleTenantId.Value)];
            }

            return [.. permissions.Where(permission => permission.TenantId == 0)];
        }
    }
}
