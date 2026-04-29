#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantMemberApplicationMapper
// Guid:b4d36a5b-c85f-46c1-860e-20f75498dc0d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 租户成员应用层映射器
/// </summary>
public static class TenantMemberApplicationMapper
{
    /// <summary>
    /// 映射租户成员列表项
    /// </summary>
    /// <param name="member">租户成员关系</param>
    /// <param name="now">当前时间</param>
    /// <returns>租户成员列表项 DTO</returns>
    public static TenantMemberListItemDto ToListItemDto(SysTenantUser member, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(member);

        return new TenantMemberListItemDto
        {
            BasicId = member.BasicId,
            UserId = member.UserId,
            MemberType = member.MemberType,
            InviteStatus = member.InviteStatus,
            InvitedBy = member.InvitedBy,
            InvitedTime = member.InvitedTime,
            RespondedTime = member.RespondedTime,
            EffectiveTime = member.EffectiveTime,
            ExpirationTime = member.ExpirationTime,
            LastActiveTime = member.LastActiveTime,
            DisplayName = member.DisplayName,
            Status = member.Status,
            IsExpired = member.ExpirationTime.HasValue && member.ExpirationTime.Value <= now,
            CreatedTime = member.CreatedTime,
            ModifiedTime = member.ModifiedTime
        };
    }

    /// <summary>
    /// 映射租户成员详情
    /// </summary>
    /// <param name="member">租户成员关系</param>
    /// <param name="now">当前时间</param>
    /// <returns>租户成员详情 DTO</returns>
    public static TenantMemberDetailDto ToDetailDto(SysTenantUser member, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(member);

        return new TenantMemberDetailDto
        {
            BasicId = member.BasicId,
            UserId = member.UserId,
            MemberType = member.MemberType,
            InviteStatus = member.InviteStatus,
            InvitedBy = member.InvitedBy,
            InvitedTime = member.InvitedTime,
            RespondedTime = member.RespondedTime,
            EffectiveTime = member.EffectiveTime,
            ExpirationTime = member.ExpirationTime,
            LastActiveTime = member.LastActiveTime,
            DisplayName = member.DisplayName,
            InviteRemark = member.InviteRemark,
            Status = member.Status,
            IsExpired = member.ExpirationTime.HasValue && member.ExpirationTime.Value <= now,
            Remark = member.Remark,
            CreatedTime = member.CreatedTime,
            CreatedId = member.CreatedId,
            CreatedBy = member.CreatedBy,
            ModifiedTime = member.ModifiedTime,
            ModifiedId = member.ModifiedId,
            ModifiedBy = member.ModifiedBy
        };
    }
}
