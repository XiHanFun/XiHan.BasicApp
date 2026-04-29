#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasPermissionSeeder
// Guid:17c438e7-8688-4f72-92ca-084ab456d513
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 权限种子数据
/// </summary>
public sealed class SaasPermissionSeeder : DataSeederBase
{
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientResolver">SqlSugar 客户端解析器</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentTenant">当前租户上下文</param>
    public SaasPermissionSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SaasPermissionSeeder> logger,
        IServiceProvider serviceProvider,
        ICurrentTenant currentTenant)
        : base(clientResolver, logger, serviceProvider)
    {
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 20;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]系统权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var definitions = BuildDefinitions();
        var permissionCodes = definitions.Select(definition => definition.PermissionCode).ToArray();
        var existingCodes = await client.Queryable<SysPermission>()
            .Where(permission => permission.IsGlobal && permissionCodes.Contains(permission.PermissionCode))
            .Select(permission => permission.PermissionCode)
            .ToListAsync();

        var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var addList = definitions
            .Where(definition => !existingSet.Contains(definition.PermissionCode))
            .Select(definition => new SysPermission
            {
                PermissionType = PermissionType.Functional,
                ModuleCode = definition.ModuleCode,
                PermissionCode = definition.PermissionCode,
                PermissionName = definition.PermissionName,
                PermissionDescription = definition.PermissionDescription,
                Tags = definition.Tags,
                IsRequireAudit = definition.IsRequireAudit,
                IsGlobal = true,
                Priority = definition.Priority,
                Status = EnableStatus.Enabled,
                Sort = definition.Sort
            })
            .ToList();

        if (addList.Count == 0)
        {
            Logger.LogInformation("SaaS 权限数据已存在，跳过种子数据");
            return;
        }

        await client.Insertable(addList).ExecuteReturnSnowflakeIdListAsync();
        Logger.LogInformation("成功初始化 {Count} 个 SaaS 权限", addList.Count);
    }

    private static IReadOnlyList<PermissionSeedDefinition> BuildDefinitions()
    {
        return
        [
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.Tenant.Read,
                "租户查看",
                "查看当前用户可进入的租户列表",
                "[\"saas\",\"tenant\"]",
                false,
                100,
                100),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.Tenant.Create,
                "租户创建",
                "创建租户基础资料",
                "[\"saas\",\"tenant\"]",
                true,
                110,
                110),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.Tenant.Update,
                "租户更新",
                "更新租户基础资料",
                "[\"saas\",\"tenant\"]",
                true,
                120,
                120),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.Tenant.Status,
                "租户状态",
                "更新租户生命周期状态",
                "[\"saas\",\"tenant\"]",
                true,
                130,
                130),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantMember.Read,
                "租户成员查看",
                "查看当前租户成员列表和详情",
                "[\"saas\",\"tenant-member\"]",
                false,
                135,
                135),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantMember.Update,
                "租户成员更新",
                "更新当前租户成员资料和有效期",
                "[\"saas\",\"tenant-member\"]",
                true,
                136,
                136),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantMember.Status,
                "租户成员状态",
                "更新当前租户成员启停状态",
                "[\"saas\",\"tenant-member\"]",
                true,
                137,
                137),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantMember.InviteStatus,
                "租户成员邀请状态",
                "更新当前租户成员邀请生命周期状态",
                "[\"saas\",\"tenant-member\"]",
                true,
                138,
                138),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantMember.Revoke,
                "租户成员撤销",
                "撤销当前租户成员身份",
                "[\"saas\",\"tenant-member\"]",
                true,
                139,
                139),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantEdition.Read,
                "租户版本查看",
                "查看租户版本套餐列表和详情",
                "[\"saas\",\"tenant-edition\"]",
                false,
                140,
                140),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantEdition.Create,
                "租户版本创建",
                "创建租户版本套餐",
                "[\"saas\",\"tenant-edition\"]",
                true,
                150,
                150),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantEdition.Update,
                "租户版本更新",
                "更新租户版本套餐",
                "[\"saas\",\"tenant-edition\"]",
                true,
                160,
                160),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantEdition.Status,
                "租户版本状态",
                "更新租户版本上下架状态",
                "[\"saas\",\"tenant-edition\"]",
                true,
                170,
                170),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantEdition.Default,
                "租户版本默认",
                "设置默认租户版本",
                "[\"saas\",\"tenant-edition\"]",
                true,
                180,
                180),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantEditionPermission.Read,
                "租户版本权限查看",
                "查看租户版本可用权限绑定",
                "[\"saas\",\"tenant-edition-permission\"]",
                false,
                190,
                190),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantEditionPermission.Grant,
                "租户版本权限授权",
                "授予租户版本可用权限",
                "[\"saas\",\"tenant-edition-permission\"]",
                true,
                200,
                200),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantEditionPermission.Update,
                "租户版本权限更新",
                "更新租户版本权限绑定状态",
                "[\"saas\",\"tenant-edition-permission\"]",
                true,
                210,
                210),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.TenantEditionPermission.Revoke,
                "租户版本权限撤销",
                "撤销租户版本可用权限",
                "[\"saas\",\"tenant-edition-permission\"]",
                true,
                220,
                220),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.Permission.Read,
                "权限定义查看",
                "查看权限定义列表、详情和全局权限选择项",
                "[\"saas\",\"permission\"]",
                false,
                230,
                230),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.Resource.Read,
                "资源定义查看",
                "查看资源定义列表、详情和全局资源选择项",
                "[\"saas\",\"resource\"]",
                false,
                240,
                240),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.Operation.Read,
                "操作定义查看",
                "查看操作定义列表、详情和全局操作选择项",
                "[\"saas\",\"operation\"]",
                false,
                250,
                250),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.Role.Read,
                "角色定义查看",
                "查看角色定义列表、详情和已启用角色选择项",
                "[\"saas\",\"role\"]",
                false,
                260,
                260),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.Role.Create,
                "角色定义创建",
                "创建当前租户角色定义",
                "[\"saas\",\"role\"]",
                true,
                262,
                262),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.Role.Update,
                "角色定义更新",
                "更新当前租户角色基础资料",
                "[\"saas\",\"role\"]",
                true,
                264,
                264),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.Role.Status,
                "角色定义状态",
                "更新当前租户角色启停状态",
                "[\"saas\",\"role\"]",
                true,
                266,
                266),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.Role.Delete,
                "角色定义删除",
                "删除当前租户未被引用的角色定义",
                "[\"saas\",\"role\"]",
                true,
                268,
                268),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.RoleHierarchy.Read,
                "角色继承查看",
                "查看角色继承祖先链、后代链和详情",
                "[\"saas\",\"role-hierarchy\"]",
                false,
                269,
                269),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.RolePermission.Read,
                "角色权限查看",
                "查看角色权限绑定列表和详情",
                "[\"saas\",\"role-permission\"]",
                false,
                270,
                270),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.RolePermission.Grant,
                "角色权限授权",
                "授予或拒绝角色权限",
                "[\"saas\",\"role-permission\"]",
                true,
                280,
                280),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.RolePermission.Update,
                "角色权限更新",
                "更新角色权限操作和有效期",
                "[\"saas\",\"role-permission\"]",
                true,
                290,
                290),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.RolePermission.Status,
                "角色权限状态",
                "更新角色权限绑定状态",
                "[\"saas\",\"role-permission\"]",
                true,
                300,
                300),
            new(
                SaasPermissionCodes.Module,
                SaasPermissionCodes.RolePermission.Revoke,
                "角色权限撤销",
                "撤销角色权限绑定",
                "[\"saas\",\"role-permission\"]",
                true,
                310,
                310)
        ];
    }

    private sealed record PermissionSeedDefinition(
        string ModuleCode,
        string PermissionCode,
        string PermissionName,
        string PermissionDescription,
        string Tags,
        bool IsRequireAudit,
        int Priority,
        int Sort);
}
