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
    public override int Order => 12;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => $"[Rbac]系统角色权限关系种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        if (await HasDataAsync<SysRolePermission>(rp => true))
        {
            Logger.LogInformation("系统角色权限关系数据已存在，跳过种子数据");
            return;
        }

        var roles = await DbContext.GetClient().Queryable<SysRole>().ToListAsync();
        var permissions = await DbContext.GetClient().Queryable<SysPermission>().ToListAsync();

        if (roles.Count == 0 || permissions.Count == 0)
        {
            Logger.LogWarning("角色或权限数据不存在，跳过角色权限关系种子数据");
            return;
        }

        var rolePermissions = new List<SysRolePermission>();

        // 超级管理员拥有所有权限
        var superAdminRole = roles.FirstOrDefault(r => r.RoleCode == "super_admin");
        if (superAdminRole != null)
        {
            rolePermissions.AddRange(permissions.Select(p => new SysRolePermission
            {
                RoleId = superAdminRole.BasicId,
                PermissionId = p.BasicId
            }));
        }

        // 系统管理员拥有管理权限（排除敏感的删除和撤销权限）
        var adminRole = roles.FirstOrDefault(r => r.RoleCode == "admin");
        if (adminRole != null)
        {
            var adminPermissions = permissions.Where(p =>
                !p.PermissionCode.Contains(":delete") &&
                !p.PermissionCode.Contains(":revoke") &&
                !p.PermissionCode.Contains("tenant:") // 排除租户管理
            ).ToList();

            rolePermissions.AddRange(adminPermissions.Select(p => new SysRolePermission
            {
                RoleId = adminRole.BasicId,
                PermissionId = p.BasicId
            }));
        }

        // 部门管理员权限（用户、角色、部门的管理权限）
        var deptAdminRole = roles.FirstOrDefault(r => r.RoleCode == "dept_admin");
        if (deptAdminRole != null)
        {
            var deptAdminPermissions = permissions.Where(p =>
                p.PermissionCode.StartsWith("user:") ||
                p.PermissionCode.StartsWith("department:") ||
                (p.PermissionCode.StartsWith("role:") && !p.PermissionCode.Contains(":delete"))
            ).ToList();

            rolePermissions.AddRange(deptAdminPermissions.Select(p => new SysRolePermission
            {
                RoleId = deptAdminRole.BasicId,
                PermissionId = p.BasicId
            }));
        }

        // 部门经理权限（查看和基本管理）
        var deptManagerRole = roles.FirstOrDefault(r => r.RoleCode == "dept_manager");
        if (deptManagerRole != null)
        {
            var deptManagerPermissions = permissions.Where(p =>
                p.PermissionCode.Contains(":read") ||
                p.PermissionCode.Contains(":view") ||
                (p.PermissionCode.StartsWith("user:") && (p.PermissionCode.Contains(":update") || p.PermissionCode.Contains(":create")))
            ).ToList();

            rolePermissions.AddRange(deptManagerPermissions.Select(p => new SysRolePermission
            {
                RoleId = deptManagerRole.BasicId,
                PermissionId = p.BasicId
            }));
        }

        // 普通员工权限（仅查看）
        var employeeRole = roles.FirstOrDefault(r => r.RoleCode == "employee");
        if (employeeRole != null)
        {
            var employeePermissions = permissions.Where(p =>
                p.PermissionCode.Contains(":read") ||
                p.PermissionCode.Contains(":view")
            ).ToList();

            rolePermissions.AddRange(employeePermissions.Select(p => new SysRolePermission
            {
                RoleId = employeeRole.BasicId,
                PermissionId = p.BasicId
            }));
        }

        // 访客权限（极少数查看权限）
        var guestRole = roles.FirstOrDefault(r => r.RoleCode == "guest");
        if (guestRole != null)
        {
            var guestPermissions = permissions.Where(p =>
                p.PermissionCode == "user:read" ||
                p.PermissionCode == "department:read"
            ).ToList();

            rolePermissions.AddRange(guestPermissions.Select(p => new SysRolePermission
            {
                RoleId = guestRole.BasicId,
                PermissionId = p.BasicId
            }));
        }

        await BulkInsertAsync(rolePermissions);
        Logger.LogInformation($"成功初始化 {rolePermissions.Count} 个角色权限关系");
    }
}
