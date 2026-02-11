#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserRoleSeeder
// Guid:9f0a1b2c-3d4e-5f6a-7b8c-9d0e1f2a3b4c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/05 00:00:00
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
    public override int Order => 15;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Rbac]系统用户角色关系种子数据";

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

        // 获取用户
        var users = await DbContext.GetClient().Queryable<SysUser>().ToListAsync();
        var roles = await DbContext.GetClient().Queryable<SysRole>().ToListAsync();

        if (users.Count == 0 || roles.Count == 0)
        {
            Logger.LogWarning("找不到用户或角色数据，跳过用户角色关系种子数据");
            return;
        }

        var userMap = users.ToDictionary(u => u.UserName, u => u.BasicId);
        var roleMap = roles.ToDictionary(r => r.RoleCode, r => r.BasicId);

        var userRoles = new List<SysUserRole>();

        // admin -> super_admin
        AddUserRole(userRoles, userMap, roleMap, "superadmin", "super_admin");

        // system -> admin
        AddUserRole(userRoles, userMap, roleMap, "systemadmin", "admin");

        // test -> employee
        AddUserRole(userRoles, userMap, roleMap, "test", "employee");

        // demo -> guest
        AddUserRole(userRoles, userMap, roleMap, "demo", "guest");

        await BulkInsertAsync(userRoles);
        Logger.LogInformation($"成功初始化 {userRoles.Count} 个用户角色关系");
    }

    /// <summary>
    /// 添加用户角色关系
    /// </summary>
    private static void AddUserRole(List<SysUserRole> userRoles, Dictionary<string, long> userMap, Dictionary<string, long> roleMap, string userName, string roleCode)
    {
        if (userMap.TryGetValue(userName, out var userId) && roleMap.TryGetValue(roleCode, out var roleId))
        {
            userRoles.Add(new SysUserRole
            {
                UserId = userId,
                RoleId = roleId
            });
        }
    }
}
