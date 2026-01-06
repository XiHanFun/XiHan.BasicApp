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
    public SysRoleSeeder(ISqlSugarDbContext dbContext, ILogger<SysRoleSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
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
    /// 种子数据实现
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
            // 超级管理员
            new()
            {
                ParentRoleId = null,
                RoleCode = "super_admin",
                RoleName = "超级管理员",
                RoleDescription = "系统最高权限角色，拥有所有功能权限",
                RoleType = RoleType.System,
                DataScope = DataPermissionScope.All,
                Status = YesOrNo.Yes,
                Sort = 1
            },
            // 系统管理员
            new()
            {
                ParentRoleId = null,
                RoleCode = "admin",
                RoleName = "系统管理员",
                RoleDescription = "系统管理员，拥有系统配置和管理权限",
                RoleType = RoleType.System,
                DataScope = DataPermissionScope.All,
                Status = YesOrNo.Yes,
                Sort = 2
            },
            // 部门管理员
            new()
            {
                ParentRoleId = null,
                RoleCode = "dept_admin",
                RoleName = "部门管理员",
                RoleDescription = "部门管理员，管理本部门及子部门数据",
                RoleType = RoleType.Custom,
                DataScope = DataPermissionScope.DepartmentAndChildren,
                Status = YesOrNo.Yes,
                Sort = 10
            },
            // 部门经理
            new()
            {
                ParentRoleId = null,
                RoleCode = "dept_manager",
                RoleName = "部门经理",
                RoleDescription = "部门经理，管理本部门数据",
                RoleType = RoleType.Custom,
                DataScope = DataPermissionScope.DepartmentOnly,
                Status = YesOrNo.Yes,
                Sort = 11
            },
            // 普通员工
            new()
            {
                ParentRoleId = null,
                RoleCode = "employee",
                RoleName = "普通员工",
                RoleDescription = "普通员工，查看和管理自己的数据",
                RoleType = RoleType.Custom,
                DataScope = DataPermissionScope.SelfOnly,
                Status = YesOrNo.Yes,
                Sort = 20
            },
            // 访客
            new()
            {
                ParentRoleId = null,
                RoleCode = "guest",
                RoleName = "访客",
                RoleDescription = "访客角色，仅拥有基础查看权限",
                RoleType = RoleType.Custom,
                DataScope = DataPermissionScope.SelfOnly,
                Status = YesOrNo.Yes,
                Sort = 30
            }
        };

        await BulkInsertAsync(roles);
        Logger.LogInformation($"成功初始化 {roles.Count} 个系统角色");
    }
}
