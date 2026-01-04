#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleSeeder
// Guid:7d8e9f0a-1b2c-3d4e-5f6a-7b8c9d0e1f2a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025-01-05 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Rbac.Seeders;

/// <summary>
/// 系统角色种子数据
/// </summary>
public class SysRoleSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysRoleSeeder(ISqlSugarDbContext dbContext, ILogger<SysRoleSeeder> logger)
        : base(dbContext, logger)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 10;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "系统角色种子数据";

    /// <summary>
    /// 种子数据实现（异步）
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        // 检查是否已有角色数据
        if (await HasDataAsync<SysRole>(r => true))
        {
            Logger.LogInformation("系统角色数据已存在，跳过种子数据");
            return;
        }

        var roles = new List<SysRole>
        {
            new()
            {
                RoleCode = "SuperAdmin",
                RoleName = "超级管理员",
                RoleDescription = "系统超级管理员，拥有所有权限",
                RoleType = RoleType.System,
                DataScope = DataPermissionScope.All,
                Status = YesOrNo.Yes,
                Sort = 0
            },
            new()
            {
                RoleCode = "Admin",
                RoleName = "管理员",
                RoleDescription = "系统管理员，拥有大部分管理权限",
                RoleType = RoleType.System,
                DataScope = DataPermissionScope.Custom,
                Status = YesOrNo.Yes,
                Sort = 1
            },
            new()
            {
                RoleCode = "User",
                RoleName = "普通用户",
                RoleDescription = "系统普通用户",
                RoleType = RoleType.Custom,
                DataScope = DataPermissionScope.SelfOnly,
                Status = YesOrNo.Yes,
                Sort = 2
            }
        };

        await BulkInsertAsync(roles);
    }
}
