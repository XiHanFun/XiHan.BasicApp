#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSeeder
// Guid:8e9f0a1b-2c3d-4e5f-6a7b-8c9d0e1f2a3b
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
using XiHan.Framework.Utils.Security.Cryptography;

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
    public override int Order => 20;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "系统用户种子数据";

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

        var users = new List<SysUser>
        {
            new()
            {
                UserName = "admin",
                Password = HashHelper.Md5("Admin@123"),
                RealName = "系统管理员",
                NickName = "Admin",
                Gender = UserGender.Unknown,
                Email = "admin@xihan.com",
                Phone = "13800138000",
                Avatar = "/assets/avatars/admin.png",
                Status = YesOrNo.Yes
            },
            new()
            {
                UserName = "test",
                Password = HashHelper.Md5("Test@123"),
                RealName = "测试用户",
                NickName = "Test",
                Gender = UserGender.Unknown,
                Email = "test@xihan.com",
                Phone = "13800138001",
                Status = YesOrNo.Yes
            }
        };

        await BulkInsertAsync(users);
    }
}
