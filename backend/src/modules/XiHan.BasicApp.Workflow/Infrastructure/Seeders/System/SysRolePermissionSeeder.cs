// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Workflow.Infrastructure.Seeders.System;

/// <summary>
/// 系统角色权限种子数据
/// </summary>
/// <remarks>
/// 工作流是租户侧能力，平台超管用不到：默认授给各租户的系统管理员（tenant_admin），其余业务角色的
/// workflow:* 授权由租户管理员在角色管理界面按需分配；之后开通的租户由所有者角色按套餐白名单获得。
/// 待办办理不依赖 workflow:* 权限（登录即可，受理人归属服务端校验），普通审批人无需任何授权。
/// </remarks>
public class SysRolePermissionSeeder : PlatformDataSeederBase
{
    private const string TenantAdminRoleCode = "tenant_admin";

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
    public override int Order => 304;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Workflow]系统角色权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var permissions = await client.Queryable<SysPermission>()
            .Where(p => p.TenantId == 0 && p.PermissionCode.StartsWith("workflow:"))
            .ToListAsync();
        if (permissions.Count == 0)
        {
            Logger.LogWarning("工作流权限不存在，跳过工作流角色权限种子");
            return;
        }

        var tenantGrantableIds = permissions
            .Where(p => p.Side.IsTenantEffective())
            .Select(p => p.BasicId)
            .ToList();
        var tenantIds = await client.Queryable<SysTenant>().Select(t => t.BasicId).ToListAsync();

        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();
        var grantedCount = 0;
        foreach (var tenantId in tenantIds)
        {
            using var tenantScope = currentTenant.Change(tenantId, tenantId.ToString());
            grantedCount += await GrantTenantAdminAsync(DbClient, tenantId, tenantGrantableIds);
        }

        Logger.LogInformation("工作流权限授予各租户系统管理员：新增角色权限 {GrantCount} 条", grantedCount);
    }

    /// <summary>
    /// 给租户的系统管理员补齐缺失的工作流权限绑定
    /// </summary>
    private static async Task<int> GrantTenantAdminAsync(ISqlSugarClient client, long tenantId, IReadOnlyCollection<long> permissionIds)
    {
        if (permissionIds.Count == 0)
        {
            return 0;
        }

        var role = await client.Queryable<SysRole>()
            .FirstAsync(r => r.TenantId == tenantId && r.RoleCode == TenantAdminRoleCode);
        if (role is null)
        {
            return 0;
        }

        var existingIds = (await client.Queryable<SysRolePermission>()
                .Where(rp => rp.TenantId == tenantId && rp.RoleId == role.BasicId)
                .ToListAsync())
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        var addList = permissionIds
            .Where(id => !existingIds.Contains(id))
            .Select(id => new SysRolePermission
            {
                TenantId = tenantId,
                RoleId = role.BasicId,
                PermissionId = id,
                PermissionAction = PermissionAction.Grant,
                Status = ValidityStatus.Valid,
                GrantReason = "系统初始化工作流模块角色权限"
            })
            .ToList();

        if (addList.Count > 0)
        {
            _ = await client.Insertable(addList).ExecuteReturnSnowflakeIdListAsync();
        }

        return addList.Count;
    }
}
