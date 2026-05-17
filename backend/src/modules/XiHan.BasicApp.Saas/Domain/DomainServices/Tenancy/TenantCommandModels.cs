#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantCommandModels
// Guid:2b7754b1-c814-412d-bcbd-d2c059a66932
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户创建命令
/// </summary>
public sealed record TenantCreateCommand(
    string TenantCode,
    string TenantName,
    string? TenantShortName,
    string? Logo,
    string? Domain,
    long? EditionId,
    TenantIsolationMode IsolationMode,
    DateTimeOffset? ExpireTime,
    int? UserLimit,
    long? StorageLimit,
    int Sort,
    string? Remark);

/// <summary>
/// 租户更新命令
/// </summary>
public sealed record TenantUpdateCommand(
    long BasicId,
    string TenantName,
    string? TenantShortName,
    string? Logo,
    string? Domain,
    long? EditionId,
    TenantIsolationMode IsolationMode,
    DateTimeOffset? ExpireTime,
    int? UserLimit,
    long? StorageLimit,
    int Sort,
    string? Remark);

/// <summary>
/// 租户状态变更命令
/// </summary>
public sealed record TenantStatusChangeCommand(long BasicId, TenantStatus TenantStatus, string? Reason, long? OperatorUserId);

/// <summary>
/// 租户成员更新命令
/// </summary>
public sealed record TenantMemberUpdateCommand(
    long BasicId,
    TenantMemberType MemberType,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? DisplayName,
    string? InviteRemark,
    string? Remark);

/// <summary>
/// 租户成员邀请状态变更命令
/// </summary>
public sealed record TenantMemberInviteStatusChangeCommand(long BasicId, TenantMemberInviteStatus InviteStatus, string? InviteRemark);

/// <summary>
/// 租户成员状态变更命令
/// </summary>
public sealed record TenantMemberStatusChangeCommand(long BasicId, ValidityStatus Status, string? Remark);

/// <summary>
/// 租户命令结果
/// </summary>
public sealed record TenantCommandResult(SysTenant Tenant, DateTimeOffset Now);

/// <summary>
/// 租户成员命令结果
/// </summary>
public sealed record TenantMemberCommandResult(SysTenantUser Member, DateTimeOffset Now);
