#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantUserSeeder
// Guid:17823510-ea32-43f9-b5bc-6ef7d55650f3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统租户成员种子数据。
/// </summary>
public class SysTenantUserSeeder : DataSeederBase
{
    public SysTenantUserSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysTenantUserSeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.TenantMemberships;

    public override string Name => "[Saas]系统租户成员种子数据";

    protected override async Task SeedInternalAsync()
    {
        var users = await DbClient.Queryable<SysUser>().ToListAsync();
        if (users.Count == 0)
        {
            Logger.LogInformation("未找到任何用户，跳过租户成员种子");
            return;
        }

        var userIds = users.Select(user => user.BasicId).ToArray();
        var existingMemberships = await DbClient
            .Queryable<SysTenantUser>()
            .Where(membership => userIds.Contains(membership.UserId))
            .ToListAsync();

        var existingKeySet = existingMemberships
            .Select(membership => $"{membership.TenantId}_{membership.UserId}")
            .ToHashSet(StringComparer.Ordinal);

        var now = DateTimeOffset.UtcNow;
        var membershipsToInsert = users
            .Select(user => new SysTenantUser
            {
                TenantId = user.TenantId,
                UserId = user.BasicId,
                MemberType = ResolveMemberType(user),
                InviteStatus = TenantMemberInviteStatus.Accepted,
                RespondedTime = now,
                EffectiveTime = now,
                LastActiveTime = user.LastLoginTime,
                DisplayName = user.RealName ?? user.NickName ?? user.UserName,
                Status = YesOrNo.Yes
            })
            .Where(membership => !existingKeySet.Contains($"{membership.TenantId}_{membership.UserId}"))
            .ToList();

        if (membershipsToInsert.Count > 0)
        {
            await BulkInsertAsync(membershipsToInsert);
        }

        var expectedMembershipMap = users.ToDictionary(
            user => $"{user.TenantId}_{user.BasicId}",
            user => user,
            StringComparer.Ordinal);

        var membershipIdsToNormalize = existingMemberships
            .Where(membership =>
                expectedMembershipMap.ContainsKey($"{membership.TenantId}_{membership.UserId}")
                && (membership.Status != YesOrNo.Yes
                    || membership.InviteStatus != TenantMemberInviteStatus.Accepted))
            .Select(membership => membership.BasicId)
            .Distinct()
            .ToArray();

        if (membershipIdsToNormalize.Length > 0)
        {
            await DbClient
                .Updateable<SysTenantUser>()
                .SetColumns(membership => new SysTenantUser
                {
                    Status = YesOrNo.Yes,
                    InviteStatus = TenantMemberInviteStatus.Accepted,
                    EffectiveTime = now,
                    RespondedTime = now
                })
                .Where(membership => membershipIdsToNormalize.Contains(membership.BasicId))
                .ExecuteCommandAsync();
        }

        Logger.LogInformation(
            "租户成员种子完成：新增 {InsertCount} 条成员关系，校准 {NormalizeCount} 条成员状态",
            membershipsToInsert.Count,
            membershipIdsToNormalize.Length);
    }

    private static TenantMemberType ResolveMemberType(SysUser user)
    {
        if (user.TenantId == SaasSeedDefaults.PlatformTenantId)
        {
            return TenantMemberType.PlatformAdmin;
        }

        return user.IsSystemAccount ? TenantMemberType.Owner : TenantMemberType.Member;
    }
}
