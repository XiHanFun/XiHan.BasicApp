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

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

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
        var definitions = SaasPermissionDefinitions.All;
        var permissionCodes = definitions.Select(definition => definition.PermissionCode).ToArray();
        var existingPermissions = await client.Queryable<SysPermission>()
            .Where(permission => permission.TenantId == 0 && permissionCodes.Contains(permission.PermissionCode))
            .ToListAsync();

        var existingMap = existingPermissions
            .GroupBy(permission => permission.PermissionCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);

        var addList = new List<SysPermission>();
        var updateCount = 0;
        foreach (var definition in definitions)
        {
            if (existingMap.TryGetValue(definition.PermissionCode, out var existing))
            {
                if (ApplyDefinition(existing, definition))
                {
                    _ = await client.Updateable(existing).ExecuteCommandAsync();
                    updateCount++;
                }

                continue;
            }

            addList.Add(CreatePermission(definition));
        }

        if (addList.Count > 0)
        {
            await client.Insertable(addList).ExecuteReturnSnowflakeIdListAsync();
        }

        if (addList.Count == 0 && updateCount == 0)
        {
            Logger.LogInformation("SaaS 权限数据已存在，跳过种子数据");
            return;
        }

        Logger.LogInformation("成功初始化 SaaS 权限，新增 {AddCount} 个，更新 {UpdateCount} 个", addList.Count, updateCount);
    }

    private static SysPermission CreatePermission(SaasPermissionDefinition definition)
    {
        var permission = new SysPermission();
        _ = ApplyDefinition(permission, definition);
        return permission;
    }

    private static bool ApplyDefinition(SysPermission permission, SaasPermissionDefinition definition)
    {
        var changed = false;
        changed |= SetIfChanged(permission.TenantId, 0, value => permission.TenantId = value);
        changed |= SetIfChanged(permission.PermissionType, PermissionType.Functional, value => permission.PermissionType = value);
        changed |= SetIfChanged(permission.ResourceId, null, value => permission.ResourceId = value);
        changed |= SetIfChanged(permission.OperationId, null, value => permission.OperationId = value);
        changed |= SetIfChanged(permission.ModuleCode, definition.ModuleCode, value => permission.ModuleCode = value);
        changed |= SetIfChanged(permission.PermissionCode, definition.PermissionCode, value => permission.PermissionCode = value);
        changed |= SetIfChanged(permission.PermissionName, definition.PermissionName, value => permission.PermissionName = value);
        changed |= SetIfChanged(permission.PermissionDescription, definition.PermissionDescription, value => permission.PermissionDescription = value);
        changed |= SetIfChanged(permission.Tags, definition.Tags, value => permission.Tags = value);
        changed |= SetIfChanged(permission.IsRequireAudit, definition.IsRequireAudit, value => permission.IsRequireAudit = value);
        changed |= SetIfChanged(permission.Priority, definition.Priority, value => permission.Priority = value);
        changed |= SetIfChanged(permission.Status, EnableStatus.Enabled, value => permission.Status = value);
        changed |= SetIfChanged(permission.Sort, definition.Sort, value => permission.Sort = value);
        changed |= SetIfChanged(permission.Remark, "系统初始化全局权限", value => permission.Remark = value);
        return changed;
    }

    private static bool SetIfChanged<T>(T current, T next, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, next))
        {
            return false;
        }

        setter(next);
        return true;
    }
}
