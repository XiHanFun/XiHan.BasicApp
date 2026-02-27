#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSeeder
// Guid:8e9f0a1b-2c3d-4e5f-6a7b-8c9d0e1f2a3b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Rbac.Seeders;

/// <summary>
/// 系统用户种子数据
/// </summary>
public class SysUserSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysUserSeeder(ISqlSugarDbContext dbContext, ILogger<SysUserSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 13;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Rbac]系统用户种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        // 检查是否已有用户数据
        if (await HasDataAsync<SysUser>(u => true))
        {
            Logger.LogInformation("系统用户数据已存在，跳过种子数据");
            return;
        }

        var passwordHasher = ServiceProvider.GetRequiredService<IPasswordHasher>();

        var users = new List<SysUser>
        {
            // 超级管理员
            new()
            {
                TenantId = null,
                UserName = "superadmin",
                Password = passwordHasher.HashPassword("Admin@123"),
                RealName = "超级管理员",
                NickName = "Admin",
                Gender = UserGender.Male,
                Email = "admin@xihanfun.com",
                Phone = "13800138000",
                Avatar = "/assets/avatars/admin.png",
                Language = "zh-CN",
                Status = YesOrNo.Yes
            },
            // 系统管理员
            new()
            {
                TenantId = null,
                UserName = "systemadmin",
                Password = passwordHasher.HashPassword("System@123"),
                RealName = "系统管理员",
                NickName = "System",
                Gender = UserGender.Male,
                Email = "system@xihanfun.com",
                Phone = "13800138001",
                Avatar = "/assets/avatars/system.png",
                Language = "zh-CN",
                Status = YesOrNo.Yes
            },
            // 测试用户
            new()
            {
                TenantId = null,
                UserName = "test",
                Password = passwordHasher.HashPassword("Test@123"),
                RealName = "测试用户",
                NickName = "Test",
                Gender = UserGender.Unknown,
                Email = "test@xihanfun.com",
                Phone = "13800138002",
                Avatar = "/assets/avatars/default.png",
                Language = "zh-CN",
                Status = YesOrNo.Yes
            },
            // 演示用户
            new()
            {
                TenantId = null,
                UserName = "demo",
                Password = passwordHasher.HashPassword("Demo@123"),
                RealName = "演示用户",
                NickName = "Demo",
                Gender = UserGender.Unknown,
                Email = "demo@xihanfun.com",
                Phone = "13800138003",
                Avatar = "/assets/avatars/default.png",
                Language = "zh-CN",
                Status = YesOrNo.Yes
            }
        };

        await BulkInsertAsync(users);
        Logger.LogInformation($"成功初始化 {users.Count} 个系统用户");
    }
}
