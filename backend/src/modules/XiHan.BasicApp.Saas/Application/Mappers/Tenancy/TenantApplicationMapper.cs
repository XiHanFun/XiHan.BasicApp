#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantApplicationMapper
// Guid:9f6f50f2-8ea8-4e9a-882c-4b8c5bc2a95c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 租户应用层映射器
/// </summary>
public static class TenantApplicationMapper
{
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
            ExpireTime = tenant.ExpireTime,
            MembershipId = membership.BasicId,
            MemberType = membership.MemberType,
            InviteStatus = membership.InviteStatus,
            MembershipExpirationTime = membership.ExpirationTime,
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
            ConfigStatus = tenant.ConfigStatus,
            TenantStatus = tenant.TenantStatus,
            ExpireTime = tenant.ExpireTime,
            IsExpired = tenant.ExpireTime.HasValue && tenant.ExpireTime.Value <= now,
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
            ConfigStatus = tenant.ConfigStatus,
            TenantStatus = tenant.TenantStatus,
            ExpireTime = tenant.ExpireTime,
            IsExpired = tenant.ExpireTime.HasValue && tenant.ExpireTime.Value <= now,
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
