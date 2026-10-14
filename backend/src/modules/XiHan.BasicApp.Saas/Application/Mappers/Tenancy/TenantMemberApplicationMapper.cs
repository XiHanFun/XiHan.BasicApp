// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 租户成员应用层映射器
/// </summary>
public static class TenantMemberApplicationMapper
{
    /// <summary>
    /// 解析成员在租户内的显示名
    /// </summary>
    /// <remarks>
    /// 成员名片（租户内显示名）优先，没填时取账号的真实姓名 → 昵称 → 用户名，与审批中心解析人名的口径一致。
    /// 成员关系与账号都查不到时返回 null，由调用方决定怎么显示。
    /// </remarks>
    /// <param name="member">租户成员关系</param>
    /// <param name="user">成员账号（外部成员的账号注册在别的租户，调用方需跨租户读取）</param>
    /// <returns>显示名</returns>
    public static string? ToDisplayName(SysTenantUser? member, SysUser? user)
    {
        if (!string.IsNullOrWhiteSpace(member?.DisplayName))
        {
            return member.DisplayName;
        }

        if (user is null)
        {
            return null;
        }

        return !string.IsNullOrWhiteSpace(user.RealName) ? user.RealName
            : !string.IsNullOrWhiteSpace(user.NickName) ? user.NickName
            : user.UserName;
    }

    /// <summary>
    /// 映射租户成员添加命令（直接生效）
    /// </summary>
    public static TenantMemberAddCommand ToAddCommand(TenantMemberAddDto input, long? operatorUserId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TenantMemberAddCommand(
            input.UserId,
            input.MemberType,
            input.EffectiveTime,
            input.ExpirationTime,
            input.DisplayName,
            null,
            input.Remark,
            RequiresInvitation: false,
            operatorUserId);
    }

    /// <summary>
    /// 映射所有权转移命令
    /// </summary>
    public static TenantOwnerTransferCommand ToOwnerTransferCommand(TenantOwnerTransferDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TenantOwnerTransferCommand(input.TenantId, input.MemberId);
    }

    /// <summary>
    /// 映射支持人员入驻命令
    /// </summary>
    public static TenantSupportMemberAddCommand ToSupportAddCommand(TenantSupportMemberAddDto input, long? operatorUserId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TenantSupportMemberAddCommand(
            input.TenantId,
            input.UserId,
            input.EffectiveTime,
            input.ExpirationTime,
            input.Remark,
            operatorUserId);
    }

    /// <summary>
    /// 映射租户成员邀请命令（待接受）
    /// </summary>
    public static TenantMemberAddCommand ToInviteCommand(TenantMemberInviteDto input, long? operatorUserId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TenantMemberAddCommand(
            input.UserId,
            input.MemberType,
            input.EffectiveTime,
            input.ExpirationTime,
            input.DisplayName,
            input.InviteRemark,
            input.Remark,
            RequiresInvitation: true,
            operatorUserId);
    }

    /// <summary>
    /// 映射租户成员更新命令
    /// </summary>
    public static TenantMemberUpdateCommand ToUpdateCommand(TenantMemberUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TenantMemberUpdateCommand(
            input.BasicId,
            input.MemberType,
            input.EffectiveTime,
            input.ExpirationTime,
            input.DisplayName,
            input.InviteRemark,
            input.Remark);
    }

    /// <summary>
    /// 映射租户成员邀请状态命令
    /// </summary>
    public static TenantMemberInviteStatusChangeCommand ToInviteStatusCommand(TenantMemberInviteStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TenantMemberInviteStatusChangeCommand(input.BasicId, input.InviteStatus, input.InviteRemark);
    }

    /// <summary>
    /// 映射租户成员状态命令
    /// </summary>
    public static TenantMemberStatusChangeCommand ToStatusCommand(TenantMemberStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TenantMemberStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

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
