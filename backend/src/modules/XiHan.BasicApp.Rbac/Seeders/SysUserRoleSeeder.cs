#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserRoleSeeder
// Guid:9f0a1b2c-3d4e-5f6a-7b8c-9d0e1f2a3b4c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025-01-05 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Rbac.Seeders;

/// <summary>
/// 系统用户角色关系种子数据
/// </summary>
public class SysUserRoleSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysUserRoleSeeder(ISqlSugarDbContext dbContext, ILogger<SysUserRoleSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 30;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "系统用户角色关系种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        // 检查是否已有用户角色关系数据
        if (await HasDataAsync<SysUserRole>(ur => true))
        {
            Logger.LogInformation("系统用户角色关系数据已存在，跳过种子数据");
            return;
        }

        // 获取用户和角色
        var adminUser = await DbContext.GetClient().Queryable<SysUser>().FirstAsync(u => u.UserName == "admin");
        var testUser = await DbContext.GetClient().Queryable<SysUser>().FirstAsync(u => u.UserName == "test");

        var superAdminRole = await DbContext.GetClient().Queryable<SysRole>().FirstAsync(r => r.RoleCode == "SuperAdmin");
        var userRole = await DbContext.GetClient().Queryable<SysRole>().FirstAsync(r => r.RoleCode == "User");

        if (adminUser == null || testUser == null || superAdminRole == null || userRole == null)
        {
            Logger.LogWarning("找不到相关用户或角色数据，跳过用户角色关系种子数据");
            return;
        }

        var userRoles = new List<SysUserRole>
        {
            new()
            {
                UserId = adminUser.BasicId,
                RoleId = superAdminRole.BasicId
            },
            new()
            {
                UserId = testUser.BasicId,
                RoleId = userRole.BasicId
            }
        };

        await BulkInsertAsync(userRoles);
    }
}
