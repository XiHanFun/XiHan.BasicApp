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
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Rbac.Seeders;

/// <summary>
/// 系统角色权限关系种子数据
/// </summary>
public class SysRolePermissionSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysRolePermissionSeeder(ISqlSugarDbContext dbContext, ILogger<SysRolePermissionSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 13;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => $"[Rbac]系统角色权限关系种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var roles = await DbContext.GetClient().Queryable<SysRole>().ToListAsync();
        var permissions = await DbContext.GetClient().Queryable<SysPermission>().ToListAsync();

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
                    .OrderBy(role => role.TenantId.HasValue ? 1 : 0)
                    .ThenBy(role => role.BasicId)
                    .First()
                    .BasicId,
                StringComparer.OrdinalIgnoreCase);

        var permissionMap = permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission.PermissionCode))
            .GroupBy(permission => permission.PermissionCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(permission => permission.TenantId.HasValue ? 1 : 0)
                    .ThenBy(permission => permission.BasicId)
                    .First()
                    .BasicId,
                StringComparer.OrdinalIgnoreCase);

        var requiredPairs = new HashSet<(long RoleId, long PermissionId)>();

        if (roleMap.TryGetValue("super_admin", out var superAdminRoleId))
        {
            foreach (var permissionId in permissionMap.Values.Distinct())
            {
                requiredPairs.Add((superAdminRoleId, permissionId));
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
        var existingPairs = await DbContext.GetClient()
            .Queryable<SysRolePermission>()
            .Where(mapping => targetRoleIds.Contains(mapping.RoleId) && targetPermissionIds.Contains(mapping.PermissionId))
            .Select(mapping => new { mapping.RoleId, mapping.PermissionId })
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

        if (rolePermissions.Count == 0)
        {
            Logger.LogInformation("角色权限关系数据已存在，跳过新增");
            return;
        }

        await BulkInsertAsync(rolePermissions);
        Logger.LogInformation("成功补齐 {Count} 个角色权限关系", rolePermissions.Count);

        void AddRolePermissionPairs(string roleCode, Func<string, bool> permissionPredicate)
        {
            if (!roleMap.TryGetValue(roleCode, out var roleId))
            {
                return;
            }

            foreach (var (permissionCode, permissionId) in permissionMap)
            {
                if (permissionPredicate(permissionCode))
                {
                    requiredPairs.Add((roleId, permissionId));
                }
            }
        }
    }
}
