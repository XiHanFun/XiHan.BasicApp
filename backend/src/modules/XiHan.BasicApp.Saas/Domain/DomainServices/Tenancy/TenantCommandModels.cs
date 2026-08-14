// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    DateTimeOffset? ExpirationTime,
    int? UserLimit,
    long? StorageLimit,
    int Sort,
    string? Remark,
    TenantDatabaseType? DatabaseType,
    string? ConnectionString);

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
    DateTimeOffset? ExpirationTime,
    int? UserLimit,
    long? StorageLimit,
    int Sort,
    string? Remark,
    TenantDatabaseType? DatabaseType,
    string? ConnectionString);

/// <summary>
/// 租户状态变更命令
/// </summary>
public sealed record TenantStatusChangeCommand(long BasicId, TenantStatus TenantStatus, string? Reason, long? OperatorUserId);

/// <summary>
/// 租户成员添加命令
/// </summary>
/// <param name="TenantId">所属租户主键</param>
/// <param name="UserId">用户主键</param>
/// <param name="MemberType">成员类型</param>
/// <param name="EffectiveTime">生效时间</param>
/// <param name="ExpirationTime">失效时间</param>
/// <param name="DisplayName">租户内显示名</param>
/// <param name="InviteRemark">邀请备注</param>
/// <param name="Remark">备注</param>
/// <param name="RequiresInvitation">是否走邀请流程（true 落待处理，false 直接生效）</param>
/// <param name="OperatorUserId">操作人用户主键（邀请人）</param>
public sealed record TenantMemberAddCommand(
    long TenantId,
    long UserId,
    TenantMemberType MemberType,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? DisplayName,
    string? InviteRemark,
    string? Remark,
    bool RequiresInvitation,
    long? OperatorUserId);

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
