#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasIdentityPermissionSeeder
// Guid:59d117b9-81be-4221-bac8-d19b7bc99ed3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 基础身份权限种子数据
/// </summary>
public sealed class SaasIdentityPermissionSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasIdentityPermissionSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private const string SuperAdminRoleCode = "super_admin";

    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 21;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]基础身份权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var superAdminRole = await client.Queryable<SysRole>()
            .FirstAsync(role => role.TenantId == 0 && role.RoleCode == SuperAdminRoleCode);
        if (superAdminRole is null)
        {
            Logger.LogWarning("超级管理员角色不存在，跳过基础身份权限种子数据");
            return;
        }

        var permissions = await client.Queryable<SysPermission>()
            .Where(permission => permission.Status == EnableStatus.Enabled)
            .ToListAsync();
        if (permissions.Count == 0)
        {
            Logger.LogWarning("SaaS 权限不存在，跳过基础身份权限种子数据");
            return;
        }

        var permissionIds = permissions.Select(permission => permission.BasicId).ToArray();
        var existingPermissionIds = await client.Queryable<SysRolePermission>()
            .Where(rolePermission => rolePermission.RoleId == superAdminRole.BasicId && permissionIds.Contains(rolePermission.PermissionId))
            .Select(rolePermission => rolePermission.PermissionId)
            .ToListAsync();
        var existingSet = existingPermissionIds.ToHashSet();
        var addList = permissions
            .Where(permission => !existingSet.Contains(permission.BasicId))
            .Select(permission => new SysRolePermission
            {
                TenantId = 0,
                RoleId = superAdminRole.BasicId,
                PermissionId = permission.BasicId,
                PermissionAction = PermissionAction.Grant,
                Status = ValidityStatus.Valid,
                GrantReason = "系统初始化超级管理员权限",
                Remark = "系统初始化权限绑定"
            })
            .ToList();

        if (addList.Count == 0)
        {
            Logger.LogInformation("超级管理员权限绑定已存在，跳过种子数据");
            return;
        }

        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化超级管理员 {Count} 个权限绑定", addList.Count);
    }
}
