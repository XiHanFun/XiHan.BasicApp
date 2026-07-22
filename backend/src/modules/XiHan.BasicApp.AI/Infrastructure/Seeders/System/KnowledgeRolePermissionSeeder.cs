// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// 知识库角色权限种子数据
/// </summary>
/// <remarks>知识库属平台级开发工具：仅授予超级管理员角色。菜单可见性绑定已前移至菜单种子。</remarks>
public class KnowledgeRolePermissionSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public KnowledgeRolePermissionSeeder(ISqlSugarClientResolver clientResolver, ILogger<KnowledgeRolePermissionSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 208;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]知识库角色权限种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var permissions = await client.Queryable<SysPermission>()
            .Where(p => p.PermissionCode.StartsWith("knowledge_base:"))
            .ToListAsync();
        if (permissions.Count == 0)
        {
            Logger.LogWarning("知识库权限不存在，跳过知识库角色权限种子"); return;
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

        Logger.LogInformation("知识库仅授超级管理员：新增角色权限 {GrantCount} 条", grantedCount);
    }
}
