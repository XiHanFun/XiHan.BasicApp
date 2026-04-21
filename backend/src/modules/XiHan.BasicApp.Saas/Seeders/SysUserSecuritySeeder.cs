#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSecuritySeeder
// Guid:bc7f6b2e-f6bf-4c52-8f0d-7d8773d34c91
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统用户安全档案种子数据。
/// </summary>
public class SysUserSecuritySeeder : DataSeederBase
{
    public SysUserSecuritySeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysUserSecuritySeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.UserSecurityProfiles;

    public override string Name => "[Saas]系统用户安全档案种子数据";

    protected override async Task SeedInternalAsync()
    {
        var users = await DbClient.Queryable<SysUser>().ToListAsync();
        if (users.Count == 0)
        {
            Logger.LogInformation("未找到任何用户，跳过安全档案种子");
            return;
        }

        var userIds = users.Select(user => user.BasicId).ToArray();
        var existingUserIds = await DbClient
            .Queryable<SysUserSecurity>()
            .Where(security => userIds.Contains(security.UserId))
            .Select(security => security.UserId)
            .ToListAsync();

        var existingSet = existingUserIds.ToHashSet();
        var now = DateTimeOffset.UtcNow;
        var profiles = users
            .Where(user => !existingSet.Contains(user.BasicId))
            .Select(user => new SysUserSecurity
            {
                TenantId = user.TenantId,
                UserId = user.BasicId,
                LastPasswordChangeTime = now,
                EmailVerified = !string.IsNullOrWhiteSpace(user.Email),
                PhoneVerified = !string.IsNullOrWhiteSpace(user.Phone),
                AllowMultiLogin = true,
                MaxLoginDevices = 0
            })
            .ToList();

        if (profiles.Count == 0)
        {
            Logger.LogInformation("用户安全档案已齐备，跳过新增");
            return;
        }

        await BulkInsertAsync(profiles);
        Logger.LogInformation("成功补齐 {Count} 个用户安全档案", profiles.Count);
    }
}
