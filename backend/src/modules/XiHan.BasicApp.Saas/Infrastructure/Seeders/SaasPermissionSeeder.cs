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

        using var platformScope = _currentTenant.Change(null);
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
                100)
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
