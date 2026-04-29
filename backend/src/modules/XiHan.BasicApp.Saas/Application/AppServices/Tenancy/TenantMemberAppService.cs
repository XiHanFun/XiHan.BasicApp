#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantMemberAppService
// Guid:fa54c72e-6bb9-4e0d-84be-c3186d8cf814
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 租户成员命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "租户成员")]
public sealed class TenantMemberAppService(ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, ITenantMemberAppService
{
    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 更新租户成员
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantMember.Update)]
    public async Task<TenantMemberDetailDto> UpdateTenantMemberAsync(TenantMemberUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateMemberUpdateInput(input);

        var member = await GetTenantMemberOrThrowAsync(input.BasicId, cancellationToken);
        EnsureOwnerCanBeChanged(member, input.MemberType);
        EnsurePlatformAdminNotAssigned(input.MemberType);

        member.MemberType = input.MemberType;
        member.EffectiveTime = input.EffectiveTime;
        member.ExpirationTime = input.ExpirationTime;
        member.DisplayName = NormalizeNullable(input.DisplayName);
        member.InviteRemark = NormalizeNullable(input.InviteRemark);
        member.Remark = NormalizeNullable(input.Remark);

        var savedMember = await _tenantUserRepository.UpdateAsync(member, cancellationToken);
        return TenantMemberApplicationMapper.ToDetailDto(savedMember, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 更新租户成员状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantMember.Status)]
    public async Task<TenantMemberDetailDto> UpdateTenantMemberStatusAsync(TenantMemberStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "租户成员主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var member = await GetTenantMemberOrThrowAsync(input.BasicId, cancellationToken);
        if (member.MemberType == TenantMemberType.Owner && input.Status == ValidityStatus.Invalid)
        {
            throw new InvalidOperationException("租户所有者成员关系不能直接停用。");
        }

        member.Status = input.Status;
        member.Remark = NormalizeNullable(input.Remark);

        var savedMember = await _tenantUserRepository.UpdateAsync(member, cancellationToken);
        return TenantMemberApplicationMapper.ToDetailDto(savedMember, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 更新租户成员邀请状态
    /// </summary>
    /// <param name="input">邀请状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantMember.InviteStatus)]
    public async Task<TenantMemberDetailDto> UpdateTenantMemberInviteStatusAsync(TenantMemberInviteStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "租户成员主键必须大于 0。");
        }

        ValidateEnum(input.InviteStatus, nameof(input.InviteStatus));

        var member = await GetTenantMemberOrThrowAsync(input.BasicId, cancellationToken);
        EnsureOwnerCanBeRevoked(member, input.InviteStatus);

        member.InviteStatus = input.InviteStatus;
        member.InviteRemark = NormalizeNullable(input.InviteRemark);

        if (input.InviteStatus is TenantMemberInviteStatus.Accepted or TenantMemberInviteStatus.Rejected)
        {
            member.RespondedTime = DateTimeOffset.UtcNow;
        }

        if (input.InviteStatus is TenantMemberInviteStatus.Revoked or TenantMemberInviteStatus.Expired)
        {
            member.Status = ValidityStatus.Invalid;
            member.RespondedTime ??= DateTimeOffset.UtcNow;
        }

        var savedMember = await _tenantUserRepository.UpdateAsync(member, cancellationToken);
        return TenantMemberApplicationMapper.ToDetailDto(savedMember, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 撤销租户成员
    /// </summary>
    /// <param name="id">租户成员主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantMember.Revoke)]
    public async Task DeleteTenantMemberAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var member = await GetTenantMemberOrThrowAsync(id, cancellationToken);
        EnsureOwnerCanBeRevoked(member, TenantMemberInviteStatus.Revoked);

        member.InviteStatus = TenantMemberInviteStatus.Revoked;
        member.Status = ValidityStatus.Invalid;
        member.RespondedTime ??= DateTimeOffset.UtcNow;

        _ = await _tenantUserRepository.UpdateAsync(member, cancellationToken);
    }

    /// <summary>
    /// 获取租户成员，不存在时抛出异常
    /// </summary>
    /// <param name="id">租户成员主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员实体</returns>
    private async Task<SysTenantUser> GetTenantMemberOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "租户成员主键必须大于 0。");
        }

        return await _tenantUserRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("租户成员不存在。");
    }

    /// <summary>
    /// 校验成员更新参数
    /// </summary>
    /// <param name="input">更新参数</param>
    private static void ValidateMemberUpdateInput(TenantMemberUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "租户成员主键必须大于 0。");
        }

        ValidateEnum(input.MemberType, nameof(input.MemberType));
        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime);
    }

    /// <summary>
    /// 校验成员有效期
    /// </summary>
    /// <param name="effectiveTime">生效时间</param>
    /// <param name="expirationTime">失效时间</param>
    private static void ValidateEffectivePeriod(DateTimeOffset? effectiveTime, DateTimeOffset? expirationTime)
    {
        if (effectiveTime.HasValue && expirationTime.HasValue && expirationTime.Value <= effectiveTime.Value)
        {
            throw new InvalidOperationException("租户成员失效时间必须晚于生效时间。");
        }
    }

    /// <summary>
    /// 校验租户所有者成员类型变更
    /// </summary>
    /// <param name="member">租户成员</param>
    /// <param name="newMemberType">新的成员类型</param>
    private static void EnsureOwnerCanBeChanged(SysTenantUser member, TenantMemberType newMemberType)
    {
        if (member.MemberType == TenantMemberType.Owner && newMemberType != TenantMemberType.Owner)
        {
            throw new InvalidOperationException("租户所有者成员类型不能直接变更。");
        }
    }

    /// <summary>
    /// 校验不能通过租户成员服务分配平台管理员身份
    /// </summary>
    /// <param name="memberType">成员类型</param>
    private static void EnsurePlatformAdminNotAssigned(TenantMemberType memberType)
    {
        if (memberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员身份必须通过平台运维流程分配。");
        }
    }

    /// <summary>
    /// 校验租户所有者不能直接撤销
    /// </summary>
    /// <param name="member">租户成员</param>
    /// <param name="newInviteStatus">新的邀请状态</param>
    private static void EnsureOwnerCanBeRevoked(SysTenantUser member, TenantMemberInviteStatus newInviteStatus)
    {
        if (member.MemberType == TenantMemberType.Owner && newInviteStatus is TenantMemberInviteStatus.Revoked or TenantMemberInviteStatus.Expired)
        {
            throw new InvalidOperationException("租户所有者成员关系不能直接撤销或过期。");
        }
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="value">枚举值</param>
    /// <param name="paramName">参数名</param>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    /// <param name="value">字符串值</param>
    /// <returns>规范化后的字符串</returns>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
