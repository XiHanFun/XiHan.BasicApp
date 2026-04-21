#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleSeeder
// Guid:7d8e9f0a-1b2c-3d4e-5f6a-7b8c9d0e1f2a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统角色种子数据。
/// </summary>
public class SysRoleSeeder : DataSeederBase
{
    public SysRoleSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysRoleSeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.Roles;

    public override string Name => "[Saas]系统角色种子数据";

    protected override async Task SeedInternalAsync()
    {
        var templates = BuildTemplates();
        var roleCodes = templates.Select(item => item.RoleCode).ToArray();
        var existingRoles = await DbClient
            .Queryable<SysRole>()
            .Where(role => roleCodes.Contains(role.RoleCode))
            .ToListAsync();

        var existingMap = existingRoles.ToDictionary(role => role.RoleCode, StringComparer.OrdinalIgnoreCase);
        var toInsert = templates
            .Where(template => !existingMap.ContainsKey(template.RoleCode))
            .ToList();

        if (toInsert.Count > 0)
        {
            await BulkInsertAsync(toInsert);
        }

        var toEnableIds = existingRoles
            .Where(role =>
                role.TenantId == SaasSeedDefaults.PlatformTenantId
                && role.IsGlobal
                && role.Status != YesOrNo.Yes)
            .Select(role => role.BasicId)
            .ToArray();

        if (toEnableIds.Length > 0)
        {
            await DbClient
                .Updateable<SysRole>()
                .SetColumns(role => role.Status == YesOrNo.Yes)
                .Where(role => toEnableIds.Contains(role.BasicId))
                .ExecuteCommandAsync();
        }

        Logger.LogInformation(
            "系统角色模板种子完成：新增 {InsertCount} 项，启用 {EnableCount} 项",
            toInsert.Count,
            toEnableIds.Length);
    }

    private static List<SysRole> BuildTemplates()
    {
        return
        [
            Create("super_admin", "平台超级管理员", "平台最高权限模板，拥有所有平台与租户入口", RoleType.System, DataPermissionScope.All, 100),
            Create("platform_admin", "平台运营管理员", "平台运营、客服、风控与配置管理模板", RoleType.System, DataPermissionScope.All, 110),
            Create("tenant_owner", "租户所有者模板", "租户初始化时克隆的所有者角色模板", RoleType.System, DataPermissionScope.All, 120),
            Create("tenant_admin", "租户管理员模板", "租户初始化时克隆的管理员角色模板", RoleType.System, DataPermissionScope.DepartmentAndChildren, 130),
            Create("tenant_member", "租户成员模板", "租户初始化时克隆的成员角色模板", RoleType.System, DataPermissionScope.SelfOnly, 140),
            Create("tenant_viewer", "租户只读模板", "租户初始化时克隆的只读角色模板", RoleType.System, DataPermissionScope.SelfOnly, 150),
            Create("external_collaborator", "外部协作模板", "外部协作者或顾问的受限角色模板", RoleType.System, DataPermissionScope.SelfOnly, 160)
        ];
    }

    private static SysRole Create(
        string code,
        string name,
        string description,
        RoleType roleType,
        DataPermissionScope dataScope,
        int sort)
    {
        return new SysRole
        {
            TenantId = SaasSeedDefaults.PlatformTenantId,
            IsGlobal = true,
            RoleCode = code,
            RoleName = name,
            RoleDescription = description,
            RoleType = roleType,
            DataScope = dataScope,
            Status = YesOrNo.Yes,
            Sort = sort
        };
    }
}
