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
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统用户种子数据
/// </summary>
public class SysUserSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysUserSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysUserSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SaasSeedOrder.Users;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Saas]系统用户种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var passwordHasher = ServiceProvider.GetRequiredService<IPasswordHasher>();
        var userPasswords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [SaasSeedDefaults.BootstrapAdminUserName] = passwordHasher.HashPassword("SuperAdmin@123"),
            [SaasSeedDefaults.PlatformAdminUserName] = passwordHasher.HashPassword("Admin@123")
        };

        var bootstrapUsers = new List<SysUser>
        {
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                UserName = SaasSeedDefaults.BootstrapAdminUserName,
                RealName = "超级管理员",
                NickName = "SuperAdmin",
                Gender = UserGender.Male,
                Email = "superadmin@xihanfun.com",
                Phone = "13800138000",
                Avatar = "/assets/avatars/superadmin.png",
                Language = "zh-CN",
                Status = YesOrNo.Yes,
                IsSystemAccount = true
            },
            new()
            {
                TenantId = SaasSeedDefaults.PlatformTenantId,
                UserName = SaasSeedDefaults.PlatformAdminUserName,
                RealName = "系统管理员",
                NickName = "Admin",
                Gender = UserGender.Male,
                Email = "admin@xihanfun.com",
                Phone = "13800138001",
                Avatar = "/assets/avatars/admin.png",
                Language = "zh-CN",
                Status = YesOrNo.Yes,
                IsSystemAccount = true
            }
        };

        var existingUsers = await DbClient
            .Queryable<SysUser>()
            .Where(user =>
                user.UserName == SaasSeedDefaults.BootstrapAdminUserName
                || user.UserName == SaasSeedDefaults.PlatformAdminUserName)
            .ToListAsync();

        var existingUserMap = existingUsers.ToDictionary(user => user.UserName, StringComparer.OrdinalIgnoreCase);
        var usersToInsert = bootstrapUsers
            .Where(user => !existingUserMap.ContainsKey(user.UserName))
            .ToList();

        if (usersToInsert.Count > 0)
        {
            await BulkInsertAsync(usersToInsert);

            var insertedUsers = await DbClient
                .Queryable<SysUser>()
                .Where(user => usersToInsert.Select(u => u.UserName).Contains(user.UserName))
                .ToListAsync();

            var securityRecords = insertedUsers
                .Where(user => userPasswords.ContainsKey(user.UserName))
                .Select(user => new SysUserSecurity
                {
                    TenantId = user.TenantId,
                    UserId = user.BasicId,
                    Password = userPasswords[user.UserName],
                    LastPasswordChangeTime = DateTimeOffset.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString("N")
                })
                .ToList();

            if (securityRecords.Count > 0)
            {
                var existingSecurityUserIds = await DbClient
                    .Queryable<SysUserSecurity>()
                    .Where(s => insertedUsers.Select(u => u.BasicId).Contains(s.UserId))
                    .Select(s => s.UserId)
                    .ToListAsync();

                var newSecurityRecords = securityRecords
                    .Where(s => !existingSecurityUserIds.Contains(s.UserId))
                    .ToList();

                if (newSecurityRecords.Count > 0)
                {
                    await DbClient.Insertable(newSecurityRecords).ExecuteCommandAsync();
                }
            }
        }

        var usersToNormalize = existingUsers
            .Where(user => user.Status != YesOrNo.Yes || !user.IsSystemAccount)
            .Select(user => user.BasicId)
            .Distinct()
            .ToArray();

        if (usersToNormalize.Length > 0)
        {
            await DbClient
                .Updateable<SysUser>()
                .SetColumns(user => new SysUser
                {
                    Status = YesOrNo.Yes,
                    IsSystemAccount = true
                })
                .Where(user => usersToNormalize.Contains(user.BasicId))
                .ExecuteCommandAsync();
        }

        Logger.LogInformation(
            "系统用户种子完成：新增 {InsertCount} 个平台内置账号，校准 {NormalizeCount} 个账号状态",
            usersToInsert.Count,
            usersToNormalize.Length);
    }
}
