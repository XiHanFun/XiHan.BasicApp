// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 租户应用层映射器
/// </summary>
public static class TenantApplicationMapper
{
    /// <summary>
    /// 映射租户创建命令
    /// </summary>
    public static TenantCreateCommand ToCreateCommand(TenantCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TenantCreateCommand(
            input.TenantCode,
            input.TenantName,
            input.TenantShortName,
            input.Logo,
            input.Domain,
            input.EditionId,
            input.IsolationMode,
            input.ExpirationTime,
            input.UserLimit,
            input.StorageLimit,
            input.Sort,
            input.Remark,
            input.DatabaseType,
            input.ConnectionString);
    }

    /// <summary>
    /// 映射租户更新命令
    /// </summary>
    public static TenantUpdateCommand ToUpdateCommand(TenantUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TenantUpdateCommand(
            input.BasicId,
            input.TenantName,
            input.TenantShortName,
            input.Logo,
            input.Domain,
            input.EditionId,
            input.IsolationMode,
            input.ExpirationTime,
            input.UserLimit,
            input.StorageLimit,
            input.Sort,
            input.Remark,
            input.DatabaseType,
            input.ConnectionString);
    }

    /// <summary>
    /// 映射租户状态命令
    /// </summary>
    public static TenantStatusChangeCommand ToStatusCommand(TenantStatusUpdateDto input, long? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TenantStatusChangeCommand(input.BasicId, input.TenantStatus, input.Reason, currentUserId);
    }

    /// <summary>
    /// 映射租户切换项
    /// </summary>
    /// <param name="membership">租户成员关系</param>
    /// <param name="tenant">租户</param>
    /// <param name="currentTenantId">当前租户主键</param>
    /// <returns>租户切换项 DTO</returns>
    public static TenantSwitcherDto ToSwitcherDto(SysTenantUser membership, SysTenant tenant, long? currentTenantId)
    {
        ArgumentNullException.ThrowIfNull(membership);
        ArgumentNullException.ThrowIfNull(tenant);

        return new TenantSwitcherDto
        {
            TenantId = tenant.BasicId,
            TenantCode = tenant.TenantCode,
            TenantName = tenant.TenantName,
            TenantShortName = tenant.TenantShortName,
            Logo = tenant.Logo,
            Domain = tenant.Domain,
            TenantStatus = tenant.TenantStatus,
            ConfigStatus = tenant.ConfigStatus,
            ExpirationTime = tenant.ExpirationTime,
            MembershipId = membership.BasicId,
            MemberType = membership.MemberType,
            InviteStatus = membership.InviteStatus,
            MembershipExpirationTime = membership.ExpirationTime,
            JoinedTime = membership.RespondedTime ?? membership.CreatedTime,
            IsCurrent = currentTenantId == tenant.BasicId
        };
    }

    /// <summary>
    /// 映射租户切换项（平台账号/超级管理员）
    /// </summary>
    /// <remarks>
    /// 超级管理员是平台账号、无 SysTenantUser 成员关系，但设计上可进入任意租户。
    /// 故无成员关系可映射，成员相关字段按"平台管理员、已接受、永不过期"合成，便于前端统一展示。
    /// </remarks>
    /// <param name="tenant">租户</param>
    /// <param name="currentTenantId">当前租户主键</param>
    /// <returns>租户切换项 DTO</returns>
    public static TenantSwitcherDto ToSwitcherDto(SysTenant tenant, long? currentTenantId)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        return new TenantSwitcherDto
        {
            TenantId = tenant.BasicId,
            TenantCode = tenant.TenantCode,
            TenantName = tenant.TenantName,
            TenantShortName = tenant.TenantShortName,
            Logo = tenant.Logo,
            Domain = tenant.Domain,
            TenantStatus = tenant.TenantStatus,
            ConfigStatus = tenant.ConfigStatus,
            ExpirationTime = tenant.ExpirationTime,
            MembershipId = 0,
            MemberType = TenantMemberType.PlatformAdmin,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            MembershipExpirationTime = null,
            JoinedTime = tenant.CreatedTime,
            IsCurrent = currentTenantId == tenant.BasicId
        };
    }

    /// <summary>
    /// 映射租户列表项
    /// </summary>
    /// <param name="tenant">租户</param>
    /// <param name="now">当前时间</param>
    /// <returns>租户列表项 DTO</returns>
    public static TenantListItemDto ToListItemDto(SysTenant tenant, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        return new TenantListItemDto
        {
            BasicId = tenant.BasicId,
            TenantCode = tenant.TenantCode,
            TenantName = tenant.TenantName,
            TenantShortName = tenant.TenantShortName,
            Logo = tenant.Logo,
            Domain = tenant.Domain,
            EditionId = tenant.EditionId,
            IsolationMode = tenant.IsolationMode,
            DatabaseType = tenant.DatabaseType,
            ConfigStatus = tenant.ConfigStatus,
            TenantStatus = tenant.TenantStatus,
            ExpirationTime = tenant.ExpirationTime,
            IsExpired = tenant.ExpirationTime.HasValue && tenant.ExpirationTime.Value <= now,
            UserLimit = tenant.UserLimit,
            StorageLimit = tenant.StorageLimit,
            Sort = tenant.Sort,
            CreatedTime = tenant.CreatedTime,
            ModifiedTime = tenant.ModifiedTime
        };
    }

    /// <summary>
    /// 映射租户详情
    /// </summary>
    /// <param name="tenant">租户</param>
    /// <param name="now">当前时间</param>
    /// <returns>租户详情 DTO</returns>
    public static TenantDetailDto ToDetailDto(SysTenant tenant, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        return new TenantDetailDto
        {
            BasicId = tenant.BasicId,
            TenantCode = tenant.TenantCode,
            TenantName = tenant.TenantName,
            TenantShortName = tenant.TenantShortName,
            Logo = tenant.Logo,
            Domain = tenant.Domain,
            EditionId = tenant.EditionId,
            IsolationMode = tenant.IsolationMode,
            DatabaseType = tenant.DatabaseType,
            ConfigStatus = tenant.ConfigStatus,
            TenantStatus = tenant.TenantStatus,
            ExpirationTime = tenant.ExpirationTime,
            IsExpired = tenant.ExpirationTime.HasValue && tenant.ExpirationTime.Value <= now,
            UserLimit = tenant.UserLimit,
            StorageLimit = tenant.StorageLimit,
            Sort = tenant.Sort,
            CreatedTime = tenant.CreatedTime,
            CreatedId = tenant.CreatedId,
            CreatedBy = tenant.CreatedBy,
            ModifiedTime = tenant.ModifiedTime,
            ModifiedId = tenant.ModifiedId,
            ModifiedBy = tenant.ModifiedBy
        };
    }
}
