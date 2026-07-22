// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 审查创建命令
/// </summary>
public sealed record ReviewCreateCommand(
    string ReviewCode,
    string ReviewTitle,
    string ReviewType,
    string? ReviewContent,
    string? ReviewDescription,
    string? EntityType,
    string? EntityId,
    string? BusinessData,
    AuditStatus ReviewStatus,
    AuditResult? ReviewResult,
    int Priority,
    long? SubmitUserId,
    DateTimeOffset? SubmitTime,
    long? CurrentReviewUserId,
    string? ReviewUserIds,
    int ReviewLevel,
    int CurrentLevel,
    DateTimeOffset? ReviewStartTime,
    DateTimeOffset? ReviewEndTime,
    string? Attachments,
    string? ExtendData,
    EnableStatus Status,
    string? Remark,
    long? CurrentUserId);

/// <summary>
/// 审查更新命令
/// </summary>
public sealed record ReviewUpdateCommand(
    long BasicId,
    string ReviewTitle,
    string ReviewType,
    string? ReviewContent,
    string? ReviewDescription,
    string? EntityType,
    string? EntityId,
    string? BusinessData,
    int Priority,
    long? SubmitUserId,
    DateTimeOffset? SubmitTime,
    long? CurrentReviewUserId,
    string? ReviewUserIds,
    int ReviewLevel,
    int CurrentLevel,
    DateTimeOffset? ReviewStartTime,
    DateTimeOffset? ReviewEndTime,
    string? Attachments,
    string? ExtendData,
    string? Remark);

/// <summary>
/// 审查状态变更命令
/// </summary>
public sealed record ReviewStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 审查审核命令
/// </summary>
public sealed record ReviewAuditCommand(
    long BasicId,
    AuditResult ReviewResult,
    long? ReviewUserId,
    long? NextReviewUserId,
    string? ReviewComment,
    string? ReviewAction,
    DateTimeOffset? ReviewTime,
    string? Attachments,
    string? ExtendData,
    string? Remark,
    long? CurrentUserId);

/// <summary>
/// 审查撤回命令
/// </summary>
public sealed record ReviewWithdrawCommand(long BasicId, string? Reason, DateTimeOffset? WithdrawTime);

/// <summary>
/// 审查命令结果
/// </summary>
public sealed record ReviewCommandResult(SysReview Review);
