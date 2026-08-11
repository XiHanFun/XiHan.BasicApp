// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Chat.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Chat.Infrastructure.Seeders.System;

/// <summary>
/// 聊天模块角色权限种子数据
/// </summary>
/// <remarks>
/// 超级管理员显式授予全部聊天权限（运行时本就通配 *，此处留痕）；
/// 各租户的系统管理员（tenant_admin）授予全部可授租户权限——聊天权限码不带 saas: 前缀、
/// 不进 <c>IsTenantGrantable</c> 的默认授权面，租户默认可用性由本种子承载。
/// </remarks>
public class ChatRolePermissionSeeder : DataSeederBase
{
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatRolePermissionSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<ChatRolePermissionSeeder> logger,
        IServiceProvider serviceProvider,
        ICurrentTenant currentTenant)
        : base(clientResolver, logger, serviceProvider)
    {
        _currentTenant = currentTenant;
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 402;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Chat]系统角色权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        // 平台态：读取聊天权限与租户清单
        Dictionary<string, long> permissionIdByCode;
        List<long> tenantIds;
        long superGranted;
        using (var platformScope = _currentTenant.Change(null))
        {
            var client = DbClient;
            var permissions = await client.Queryable<SysPermission>()
                .Where(p => p.TenantId == 0 && p.PermissionCode.StartsWith(ChatPermissionCodes.Module + ":"))
                .ToListAsync();
            if (permissions.Count == 0)
            {
                Logger.LogWarning("聊天权限不存在，跳过聊天角色权限种子");
                return;
            }

            permissionIdByCode = permissions.ToDictionary(p => p.PermissionCode, p => p.BasicId, StringComparer.OrdinalIgnoreCase);
            superGranted = await GrantAsync(client, 0, "super_admin", permissionIdByCode.Values.ToList());
            tenantIds = (await client.Queryable<SysTenant>().Select(t => t.BasicId).ToListAsync()).ToList();
        }

        // 各租户态：系统管理员授予可授租户权限
        var tenantGrantableIds = ChatPermissionCodes.TenantGrantable
            .Select(code => permissionIdByCode.TryGetValue(code, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();
        var tenantGranted = 0L;
        foreach (var tenantId in tenantIds)
        {
            using var tenantScope = _currentTenant.Change(tenantId, tenantId.ToString());
            tenantGranted += await GrantAsync(DbClient, tenantId, "tenant_admin", tenantGrantableIds);
        }

        Logger.LogInformation("聊天角色权限：超级管理员新增 {SuperCount} 条，租户管理员新增 {TenantCount} 条", superGranted, tenantGranted);
    }

    /// <summary>
    /// 给指定作用域内的角色补齐缺失的权限绑定
    /// </summary>
    private static async Task<long> GrantAsync(ISqlSugarClient client, long tenantId, string roleCode, IReadOnlyCollection<long> permissionIds)
    {
        if (permissionIds.Count == 0)
        {
            return 0;
        }

        var role = await client.Queryable<SysRole>()
            .FirstAsync(r => r.TenantId == tenantId && r.RoleCode == roleCode);
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
                GrantReason = "系统初始化聊天模块角色权限"
            })
            .ToList();

        if (addList.Count > 0)
        {
            _ = await client.Insertable(addList).ExecuteReturnSnowflakeIdListAsync();
        }

        return addList.Count;
    }
}
