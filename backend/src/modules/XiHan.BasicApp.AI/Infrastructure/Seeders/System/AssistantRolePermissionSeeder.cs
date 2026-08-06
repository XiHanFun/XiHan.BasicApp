// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// AI 助手角色权限种子数据
/// </summary>
/// <remarks>助手配置属平台级开发工具：仅授予超级管理员角色。使用助手不看这组权限，只看登录态。</remarks>
public class AssistantRolePermissionSeeder : PlatformDataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public AssistantRolePermissionSeeder(ISqlSugarClientResolver clientResolver, ILogger<AssistantRolePermissionSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 216;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]助手角色权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var permissions = await client.Queryable<SysPermission>()
            .Where(p => p.PermissionCode.StartsWith("ai_assistant:"))
            .ToListAsync();
        if (permissions.Count == 0)
        {
            Logger.LogWarning("AI 助手权限不存在，跳过助手角色权限种子");
            return;
        }

        var superRole = await client.Queryable<SysRole>().FirstAsync(r => r.RoleCode == "super_admin");
        var grantedCount = 0;
        if (superRole is not null)
        {
            var permissionIds = permissions.Select(p => p.BasicId).ToList();
            var existsSet = (await client.Queryable<SysRolePermission>()
                    .Where(rp => rp.RoleId == superRole.BasicId && permissionIds.Contains(rp.PermissionId))
                    .ToListAsync())
                .Select(rp => rp.PermissionId)
                .ToHashSet();
            var addList = permissions
                .Where(p => !existsSet.Contains(p.BasicId))
                .Select(p => new SysRolePermission { RoleId = superRole.BasicId, PermissionId = p.BasicId })
                .ToList();
            if (addList.Count > 0)
            {
                await BulkInsertAsync(addList);
                grantedCount = addList.Count;
            }
        }

        Logger.LogInformation("AI 助手仅授超级管理员：新增角色权限 {GrantCount} 条", grantedCount);
    }
}
