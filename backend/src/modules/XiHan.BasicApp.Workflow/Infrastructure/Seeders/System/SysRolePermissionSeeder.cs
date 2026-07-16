#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRolePermissionSeeder
// Guid:80c52d17-e964-4f30-a2d8-63e91c40f5b7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:36:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Workflow.Infrastructure.Seeders.System;

/// <summary>
/// 系统角色权限种子数据
/// </summary>
/// <remarks>
/// 默认仅授予超级管理员（显式留痕）；业务角色的 workflow:* 授权由各租户管理员在角色管理界面按需分配。
/// 待办办理不依赖 workflow:* 权限（登录即可，受理人归属服务端校验），普通审批人无需任何授权。
/// </remarks>
public class SysRolePermissionSeeder : DataSeederBase
{
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
            .Where(p => p.PermissionCode.StartsWith("workflow:"))
            .ToListAsync();
        if (permissions.Count == 0)
        {
            Logger.LogWarning("工作流权限不存在，跳过工作流角色权限种子");
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

        Logger.LogInformation("工作流权限默认仅授超级管理员：新增角色权限 {GrantCount} 条", grantedCount);
    }
}
