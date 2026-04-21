#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserRoleSeeder
// Guid:9f0a1b2c-3d4e-5f6a-7b8c-9d0e1f2a3b4c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Constants.Basic;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统用户角色关系种子数据。
/// </summary>
public class SysUserRoleSeeder : DataSeederBase
{
    public SysUserRoleSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysUserRoleSeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.UserRoles;

    public override string Name => "[Saas]系统用户角色关系种子数据";

    protected override async Task SeedInternalAsync()
    {
        var users = await DbClient.Queryable<SysUser>().ToListAsync();
        var roles = await DbClient.Queryable<SysRole>().ToListAsync();
        if (users.Count == 0 || roles.Count == 0)
        {
            Logger.LogWarning("找不到用户或角色数据，跳过用户角色关系初始化");
            return;
        }

        var bootstrapTenant = await DbClient
            .Queryable<SysTenant>()
            .FirstAsync(tenant => tenant.TenantCode == SaasSeedDefaults.BootstrapTenantCode);

        var userMap = users.ToDictionary(user => user.UserName, StringComparer.OrdinalIgnoreCase);
        var roleMap = roles
            .GroupBy(role => $"{role.TenantId}:{role.RoleCode}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.BasicId).First(), StringComparer.OrdinalIgnoreCase);

        var requiredMappings = new List<SysUserRole>();

        if (userMap.TryGetValue(SaasSeedDefaults.BootstrapAdminUserName, out var superAdmin)
            && roleMap.TryGetValue($"{RoleBasicConstants.PlatformTenantId}:{RoleBasicConstants.SuperAdminRoleCode}", out var superAdminRole))
        {
            requiredMappings.Add(CreateMapping(superAdmin, superAdminRole, RoleBasicConstants.PlatformTenantId));
        }

        if (userMap.TryGetValue(SaasSeedDefaults.PlatformAdminUserName, out var platformAdmin)
            && roleMap.TryGetValue($"{RoleBasicConstants.PlatformTenantId}:{RoleBasicConstants.PlatformAdminRoleCode}", out var platformAdminRole))
        {
            requiredMappings.Add(CreateMapping(platformAdmin, platformAdminRole, RoleBasicConstants.PlatformTenantId));
        }

        if (bootstrapTenant is not null
            && roleMap.TryGetValue($"{RoleBasicConstants.PlatformTenantId}:tenant_owner", out var tenantOwnerRole)
            && userMap.TryGetValue(SaasSeedDefaults.PlatformAdminUserName, out var bootstrapOwner))
        {
            requiredMappings.Add(CreateMapping(bootstrapOwner, tenantOwnerRole, bootstrapTenant.BasicId));
        }

        var targetUserIds = requiredMappings.Select(item => item.UserId).Distinct().ToArray();
        var existingMappings = await DbClient
            .Queryable<SysUserRole>()
            .Where(mapping => targetUserIds.Contains(mapping.UserId))
            .ToListAsync();

        var existingSet = existingMappings
            .Select(mapping => $"{mapping.TenantId}:{mapping.UserId}:{mapping.RoleId}")
            .ToHashSet(StringComparer.Ordinal);

        var inserts = requiredMappings
            .Where(mapping => !existingSet.Contains($"{mapping.TenantId}:{mapping.UserId}:{mapping.RoleId}"))
            .ToList();

        if (inserts.Count > 0)
        {
            await BulkInsertAsync(inserts);
        }

        var disabledIds = existingMappings
            .Where(mapping =>
                requiredMappings.Any(required =>
                    required.TenantId == mapping.TenantId
                    && required.UserId == mapping.UserId
                    && required.RoleId == mapping.RoleId)
                && mapping.Status != YesOrNo.Yes)
            .Select(mapping => mapping.BasicId)
            .ToArray();

        if (disabledIds.Length > 0)
        {
            await DbClient
                .Updateable<SysUserRole>()
                .SetColumns(mapping => mapping.Status == YesOrNo.Yes)
                .Where(mapping => disabledIds.Contains(mapping.BasicId))
                .ExecuteCommandAsync();
        }

        Logger.LogInformation(
            "用户角色种子完成：新增 {InsertCount} 条，启用 {EnableCount} 条",
            inserts.Count,
            disabledIds.Length);
    }

    private static SysUserRole CreateMapping(SysUser user, SysRole role, long tenantId)
    {
        return new SysUserRole
        {
            TenantId = tenantId,
            UserId = user.BasicId,
            RoleId = role.BasicId,
            EffectiveTime = DateTimeOffset.UtcNow,
            Status = YesOrNo.Yes
        };
    }
}
