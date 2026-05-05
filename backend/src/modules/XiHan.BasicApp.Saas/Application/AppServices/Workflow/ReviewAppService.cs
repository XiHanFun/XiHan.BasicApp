#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewAppService
// Guid:fbf4199c-d6c3-4909-8910-4731ce0dc5b4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using SqlSugar;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;
using static XiHan.BasicApp.Saas.Application.AppServices.SaasCommandValidation;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统审查命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统审查")]
public sealed class ReviewAppService(
    IReviewRepository reviewRepository,
    ISqlSugarClientResolver clientResolver,
    ICurrentUser currentUser)
    : SaasApplicationService, IReviewAppService
{
    private readonly IReviewRepository _reviewRepository = reviewRepository;
    private readonly ICurrentUser _currentUser = currentUser;

    private ISqlSugarClient DbClient => clientResolver.GetCurrentClient();

    /// <summary>
    /// 创建系统审查
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Review.Create)]
    public async Task<ReviewDetailDto> CreateReviewAsync(ReviewCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);
        var reviewCode = Required(input.ReviewCode, 100, nameof(input.ReviewCode), "审查编码不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(reviewCode, "审查编码不能包含空白字符。");
        if (await _reviewRepository.AnyAsync(review => review.ReviewCode == reviewCode, cancellationToken))
        {
            throw new InvalidOperationException("审查编码已存在。");
        }

        var now = DateTimeOffset.UtcNow;
        var reviewStatus = input.ReviewStatus;
        var review = new SysReview
        {
            ReviewCode = reviewCode,
            ReviewTitle = Required(input.ReviewTitle, 200, nameof(input.ReviewTitle), "审查标题不能超过 200 个字符。"),
            ReviewType = Required(input.ReviewType, 50, nameof(input.ReviewType), "审查类型不能超过 50 个字符。"),
            ReviewContent = OptionalJson(input.ReviewContent, "审查内容必须是有效 JSON。"),
            ReviewDescription = Optional(input.ReviewDescription, 1000, nameof(input.ReviewDescription), "审查描述不能超过 1000 个字符。"),
            EntityType = Optional(input.EntityType, 100, nameof(input.EntityType), "业务实体类型不能超过 100 个字符。"),
            EntityId = Optional(input.EntityId, 100, nameof(input.EntityId), "业务实体 ID 不能超过 100 个字符。"),
            BusinessData = OptionalJson(input.BusinessData, "业务数据必须是有效 JSON。"),
            ReviewStatus = reviewStatus,
            ReviewResult = NormalizeReviewResult(input.ReviewResult, reviewStatus),
            Priority = input.Priority,
            SubmitUserId = input.SubmitUserId ?? _currentUser.UserId,
            SubmitTime = input.SubmitTime ?? now,
            CurrentReviewUserId = input.CurrentReviewUserId,
            ReviewUserIds = OptionalJson(input.ReviewUserIds, "审查人 ID 列表必须是有效 JSON。"),
            ReviewLevel = input.ReviewLevel,
            CurrentLevel = input.CurrentLevel,
            ReviewStartTime = input.ReviewStartTime,
            ReviewEndTime = ResolveReviewEndTime(input.ReviewEndTime, reviewStatus, now),
            Attachments = OptionalJson(input.Attachments, "附件信息必须是有效 JSON。"),
            ExtendData = OptionalJson(input.ExtendData, "扩展数据必须是有效 JSON。"),
            Status = input.Status,
            Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。")
        };

        var savedReview = await _reviewRepository.AddAsync(review, cancellationToken);
        return ReviewApplicationMapper.ToDetailDto(savedReview);
    }

    /// <summary>
    /// 更新系统审查
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Review.Update)]
    public async Task<ReviewDetailDto> UpdateReviewAsync(ReviewUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);
        var review = await GetReviewOrThrowAsync(input.BasicId, cancellationToken);
        if (review.ReviewStatus is AuditStatus.Approved or AuditStatus.Rejected or AuditStatus.Withdrawn)
        {
            throw new InvalidOperationException("已完结审查不能修改。");
        }

        review.ReviewTitle = Required(input.ReviewTitle, 200, nameof(input.ReviewTitle), "审查标题不能超过 200 个字符。");
        review.ReviewType = Required(input.ReviewType, 50, nameof(input.ReviewType), "审查类型不能超过 50 个字符。");
        review.ReviewContent = OptionalJson(input.ReviewContent, "审查内容必须是有效 JSON。");
        review.ReviewDescription = Optional(input.ReviewDescription, 1000, nameof(input.ReviewDescription), "审查描述不能超过 1000 个字符。");
        review.EntityType = Optional(input.EntityType, 100, nameof(input.EntityType), "业务实体类型不能超过 100 个字符。");
        review.EntityId = Optional(input.EntityId, 100, nameof(input.EntityId), "业务实体 ID 不能超过 100 个字符。");
        review.BusinessData = OptionalJson(input.BusinessData, "业务数据必须是有效 JSON。");
        review.Priority = input.Priority;
        review.SubmitUserId = input.SubmitUserId;
        review.SubmitTime = input.SubmitTime ?? review.SubmitTime;
        review.CurrentReviewUserId = input.CurrentReviewUserId;
        review.ReviewUserIds = OptionalJson(input.ReviewUserIds, "审查人 ID 列表必须是有效 JSON。");
        review.ReviewLevel = input.ReviewLevel;
        review.CurrentLevel = input.CurrentLevel;
        review.ReviewStartTime = input.ReviewStartTime;
        review.ReviewEndTime = input.ReviewEndTime;
        review.Attachments = OptionalJson(input.Attachments, "附件信息必须是有效 JSON。");
        review.ExtendData = OptionalJson(input.ExtendData, "扩展数据必须是有效 JSON。");
        review.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");

        var savedReview = await _reviewRepository.UpdateAsync(review, cancellationToken);
        return ReviewApplicationMapper.ToDetailDto(savedReview);
    }

    /// <summary>
    /// 更新系统审查启停状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Review.Status)]
    public async Task<ReviewDetailDto> UpdateReviewStatusAsync(ReviewStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统审查主键必须大于 0。");
        EnsureEnum(input.Status, nameof(input.Status));

        var review = await GetReviewOrThrowAsync(input.BasicId, cancellationToken);
        review.Status = input.Status;
        review.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? review.Remark;

        var savedReview = await _reviewRepository.UpdateAsync(review, cancellationToken);
        return ReviewApplicationMapper.ToDetailDto(savedReview);
    }

    /// <summary>
    /// 审核系统审查
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Review.Audit)]
    public async Task<ReviewDetailDto> AuditReviewAsync(ReviewAuditDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateAuditInput(input);
        var review = await GetReviewOrThrowAsync(input.BasicId, cancellationToken);
        EnsureReviewCanBeAudited(review);

        var reviewTime = input.ReviewTime ?? DateTimeOffset.UtcNow;
        var originalStatus = review.ReviewStatus;
        var newStatus = ResolveNextAuditStatus(review, input.ReviewResult);

        review.ReviewStartTime ??= reviewTime;
        review.ReviewStatus = newStatus;
        review.ReviewResult = input.ReviewResult;
        review.CurrentLevel = ResolveNextCurrentLevel(review, input.ReviewResult);
        review.CurrentReviewUserId = ResolveNextReviewUserId(review, input);
        if (newStatus is AuditStatus.Approved or AuditStatus.Rejected)
        {
            review.ReviewEndTime = reviewTime;
        }

        review.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? review.Remark;

        var savedReview = await _reviewRepository.UpdateAsync(review, cancellationToken);
        await AddReviewLogAsync(savedReview, input, originalStatus, newStatus, reviewTime, cancellationToken);
        return ReviewApplicationMapper.ToDetailDto(savedReview);
    }

    /// <summary>
    /// 撤回系统审查
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Review.Withdraw)]
    public async Task<ReviewDetailDto> WithdrawReviewAsync(ReviewWithdrawDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统审查主键必须大于 0。");
        var review = await GetReviewOrThrowAsync(input.BasicId, cancellationToken);
        if (review.ReviewStatus is AuditStatus.Approved or AuditStatus.Rejected or AuditStatus.Withdrawn)
        {
            throw new InvalidOperationException("已完结审查不能撤回。");
        }

        review.ReviewStatus = AuditStatus.Withdrawn;
        review.ReviewEndTime = input.WithdrawTime ?? DateTimeOffset.UtcNow;
        review.CurrentReviewUserId = null;
        review.Remark = Optional(input.Reason, 500, nameof(input.Reason), "撤回原因不能超过 500 个字符。") ?? review.Remark;

        var savedReview = await _reviewRepository.UpdateAsync(review, cancellationToken);
        return ReviewApplicationMapper.ToDetailDto(savedReview);
    }

    /// <summary>
    /// 删除系统审查
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Review.Delete)]
    public async Task DeleteReviewAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var review = await GetReviewOrThrowAsync(id, cancellationToken);
        if (!await _reviewRepository.DeleteAsync(review, cancellationToken))
        {
            throw new InvalidOperationException("系统审查删除失败。");
        }
    }

    private async Task<SysReview> GetReviewOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统审查主键必须大于 0。");
        return await _reviewRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统审查不存在。");
    }

    private async Task AddReviewLogAsync(
        SysReview review,
        ReviewAuditDto input,
        AuditStatus originalStatus,
        AuditStatus newStatus,
        DateTimeOffset reviewTime,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var reviewLog = new SysReviewLog
        {
            ReviewId = review.BasicId,
            ReviewLevel = review.CurrentLevel,
            ReviewUserId = input.ReviewUserId ?? _currentUser.UserId,
            OriginalStatus = originalStatus,
            NewStatus = newStatus,
            ReviewResult = input.ReviewResult,
            ReviewComment = Optional(input.ReviewComment, 1000, nameof(input.ReviewComment), "审查意见不能超过 1000 个字符。"),
            ReviewAction = Optional(input.ReviewAction, 50, nameof(input.ReviewAction), "审查动作不能超过 50 个字符。")
                ?? ResolveDefaultReviewAction(input.ReviewResult),
            Attachments = OptionalJson(input.Attachments, "审查日志附件信息必须是有效 JSON。"),
            ReviewTime = reviewTime,
            ExtendData = OptionalJson(input.ExtendData, "审查日志扩展数据必须是有效 JSON。"),
            Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。")
        };

        await DbClient.Insertable(reviewLog).SplitTable().ExecuteCommandAsync();
    }

    private static void ValidateCreateInput(ReviewCreateDto input)
    {
        ValidateReviewInput(
            input.ReviewStatus,
            input.ReviewResult,
            input.SubmitUserId,
            input.CurrentReviewUserId,
            input.ReviewLevel,
            input.CurrentLevel,
            input.ReviewStartTime,
            input.ReviewEndTime,
            input.Status);
    }

    private static void ValidateUpdateInput(ReviewUpdateDto input)
    {
        EnsureId(input.BasicId, "系统审查主键必须大于 0。");
        ValidateReviewInput(
            null,
            null,
            input.SubmitUserId,
            input.CurrentReviewUserId,
            input.ReviewLevel,
            input.CurrentLevel,
            input.ReviewStartTime,
            input.ReviewEndTime,
            null);
    }

    private static void ValidateReviewInput(
        AuditStatus? reviewStatus,
        AuditResult? reviewResult,
        long? submitUserId,
        long? currentReviewUserId,
        int reviewLevel,
        int currentLevel,
        DateTimeOffset? reviewStartTime,
        DateTimeOffset? reviewEndTime,
        EnableStatus? status)
    {
        if (reviewStatus.HasValue)
        {
            EnsureEnum(reviewStatus.Value, nameof(reviewStatus));
        }

        if (reviewResult.HasValue)
        {
            EnsureEnum(reviewResult.Value, nameof(reviewResult));
        }

        if (status.HasValue)
        {
            EnsureEnum(status.Value, nameof(status));
        }

        EnsureOptionalId(submitUserId, nameof(submitUserId), "提交人主键必须大于 0。");
        EnsureOptionalId(currentReviewUserId, nameof(currentReviewUserId), "当前审查人主键必须大于 0。");
        if (reviewLevel <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reviewLevel), "审查级别必须大于 0。");
        }

        if (currentLevel <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentLevel), "当前审查级别必须大于 0。");
        }

        if (currentLevel > reviewLevel)
        {
            throw new InvalidOperationException("当前审查级别不能大于审查级别。");
        }

        if (reviewStartTime.HasValue && reviewEndTime.HasValue && reviewEndTime.Value < reviewStartTime.Value)
        {
            throw new InvalidOperationException("审查结束时间不能早于审查开始时间。");
        }
    }

    private static void ValidateAuditInput(ReviewAuditDto input)
    {
        EnsureId(input.BasicId, "系统审查主键必须大于 0。");
        EnsureEnum(input.ReviewResult, nameof(input.ReviewResult));
        EnsureOptionalId(input.ReviewUserId, nameof(input.ReviewUserId), "审查人主键必须大于 0。");
        EnsureOptionalId(input.NextReviewUserId, nameof(input.NextReviewUserId), "下一审查人主键必须大于 0。");
        _ = Optional(input.ReviewComment, 1000, nameof(input.ReviewComment), "审查意见不能超过 1000 个字符。");
        _ = Optional(input.ReviewAction, 50, nameof(input.ReviewAction), "审查动作不能超过 50 个字符。");
        _ = OptionalJson(input.Attachments, "审查日志附件信息必须是有效 JSON。");
        _ = OptionalJson(input.ExtendData, "审查日志扩展数据必须是有效 JSON。");
        _ = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
    }

    private static AuditResult? NormalizeReviewResult(AuditResult? reviewResult, AuditStatus reviewStatus)
    {
        if (reviewResult.HasValue)
        {
            return reviewResult;
        }

        return reviewStatus switch
        {
            AuditStatus.Approved => AuditResult.Pass,
            AuditStatus.Rejected => AuditResult.Reject,
            _ => null
        };
    }

    private static DateTimeOffset? ResolveReviewEndTime(DateTimeOffset? reviewEndTime, AuditStatus reviewStatus, DateTimeOffset now)
    {
        if (reviewEndTime.HasValue)
        {
            return reviewEndTime;
        }

        return reviewStatus is AuditStatus.Approved or AuditStatus.Rejected or AuditStatus.Withdrawn ? now : null;
    }

    private static void EnsureReviewCanBeAudited(SysReview review)
    {
        if (review.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用审查不能审核。");
        }

        if (review.ReviewStatus is AuditStatus.Approved or AuditStatus.Rejected or AuditStatus.Withdrawn)
        {
            throw new InvalidOperationException("已完结审查不能审核。");
        }
    }

    private static AuditStatus ResolveNextAuditStatus(SysReview review, AuditResult reviewResult)
    {
        return reviewResult switch
        {
            AuditResult.Pass when review.CurrentLevel < review.ReviewLevel => AuditStatus.InProgress,
            AuditResult.Pass => AuditStatus.Approved,
            AuditResult.Reject => AuditStatus.Rejected,
            AuditResult.Return => AuditStatus.Pending,
            _ => throw new ArgumentOutOfRangeException(nameof(reviewResult), "审查结果无效。")
        };
    }

    private static int ResolveNextCurrentLevel(SysReview review, AuditResult reviewResult)
    {
        return reviewResult switch
        {
            AuditResult.Pass when review.CurrentLevel < review.ReviewLevel => review.CurrentLevel + 1,
            AuditResult.Return when review.CurrentLevel > 1 => review.CurrentLevel - 1,
            _ => review.CurrentLevel
        };
    }

    private static long? ResolveNextReviewUserId(SysReview review, ReviewAuditDto input)
    {
        return input.ReviewResult switch
        {
            AuditResult.Pass when review.CurrentLevel < review.ReviewLevel => input.NextReviewUserId ?? review.CurrentReviewUserId,
            AuditResult.Pass => null,
            AuditResult.Reject => null,
            AuditResult.Return => input.NextReviewUserId ?? review.SubmitUserId ?? review.CurrentReviewUserId,
            _ => review.CurrentReviewUserId
        };
    }

    private static string ResolveDefaultReviewAction(AuditResult reviewResult)
    {
        return reviewResult switch
        {
            AuditResult.Pass => "Approve",
            AuditResult.Reject => "Reject",
            AuditResult.Return => "Return",
            _ => "Audit"
        };
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
    }
}
