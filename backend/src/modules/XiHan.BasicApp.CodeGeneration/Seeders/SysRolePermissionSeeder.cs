#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRolePermissionSeeder
// Guid:46e7af60-ef56-4326-ac87-cc76f8891ace
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 13:13:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.CodeGeneration.Seeders;

/// <summary>
/// 系统角色权限种子数据
/// </summary>
public class SysRolePermissionSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysRolePermissionSeeder(ISqlSugarDbContext dbContext, ILogger<SysRolePermissionSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 23;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[CodeGeneration]系统角色权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbContext.GetClient();
        var roles = await client.Queryable<SysRole>().Where(r => r.RoleCode == "super_admin" || r.RoleCode == "admin").ToListAsync();
        var permissions = await client.Queryable<SysPermission>().Where(p => p.PermissionCode.StartsWith("code_gen:") || p.PermissionCode.StartsWith("code_gen_api:")).ToListAsync();
        if (roles.Count == 0 || permissions.Count == 0)
        {
            Logger.LogWarning("系统角色或权限数据不存在，跳过系统角色权限种子数据"); return;
        }

        var pairs = roles.SelectMany(r => permissions
            .Select(p => new
            {
                RoleId = r.BasicId,
                PermissionId = p.BasicId
            })).ToList();
        var roleIds = roles.Select(r => r.BasicId).ToList();
        var permissionIds = permissions.Select(p => p.BasicId).ToList();
        var exists = await client.Queryable<SysRolePermission>()
            .Where(rp => roleIds.Contains(rp.RoleId) && permissionIds.Contains(rp.PermissionId))
            .ToListAsync();
        var existsSet = exists.Select(e => $"{e.RoleId}-{e.PermissionId}").ToHashSet();
        var addList = pairs.Where(x => !existsSet.Contains($"{x.RoleId}-{x.PermissionId}"))
            .Select(x => new SysRolePermission
            {
                RoleId = x.RoleId,
                PermissionId = x.PermissionId
            }).ToList();

        if (addList.Count == 0)
        {
            Logger.LogInformation("系统角色权限数据已存在，跳过种子数据"); return;
        }
        await BulkInsertAsync(addList);
        Logger.LogInformation("成功初始化 {Count} 个系统角色权限关系", addList.Count);
    }
}
