#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewDomainService
// Guid:88dd8eac-d7b8-48dd-a42c-d99d36a62b74
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 审查领域服务实现
/// </summary>
public sealed class ReviewDomainService
    : IReviewDomainService
{
    private readonly IReviewLogRepository _reviewLogRepository;

    private readonly IReviewRepository _reviewRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ReviewDomainService(
        IReviewRepository reviewRepository,
        IReviewLogRepository reviewLogRepository)
    {
        _reviewRepository = reviewRepository;
        _reviewLogRepository = reviewLogRepository;
    }

    /// <inheritdoc />
    public async Task<ReviewCommandResult> AuditReviewAsync(ReviewAuditCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateAuditCommand(command);
        var review = await GetReviewOrThrowAsync(command.BasicId, cancellationToken);
        EnsureReviewCanBeAudited(review);

        var reviewTime = command.ReviewTime ?? DateTimeOffset.UtcNow;
        var originalStatus = review.ReviewStatus;
        var newStatus = ResolveNextAuditStatus(review, command.ReviewResult);

        review.ReviewStartTime ??= reviewTime;
        review.ReviewStatus = newStatus;
        review.ReviewResult = command.ReviewResult;
        review.CurrentLevel = ResolveNextCurrentLevel(review, command.ReviewResult);
        review.CurrentReviewUserId = ResolveNextReviewUserId(review, command);
        if (newStatus is AuditStatus.Approved or AuditStatus.Rejected)
        {
            review.ReviewEndTime = reviewTime;
        }

        review.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? review.Remark;

        var savedReview = await _reviewRepository.UpdateAsync(review, cancellationToken);
        await AddReviewLogAsync(savedReview, command, originalStatus, newStatus, reviewTime, cancellationToken);
        return new ReviewCommandResult(savedReview);
    }

    /// <inheritdoc />
    public async Task<ReviewCommandResult> CreateReviewAsync(ReviewCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);
        var reviewCode = Required(command.ReviewCode, 100, nameof(command.ReviewCode), "审查编码不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(reviewCode, "审查编码不能包含空白字符。");
        if (await _reviewRepository.AnyAsync(review => review.ReviewCode == reviewCode, cancellationToken))
        {
            throw new InvalidOperationException("审查编码已存在。");
        }

        var now = DateTimeOffset.UtcNow;
        var reviewStatus = command.ReviewStatus;
        var review = new SysReview
        {
            ReviewCode = reviewCode,
            ReviewTitle = Required(command.ReviewTitle, 200, nameof(command.ReviewTitle), "审查标题不能超过 200 个字符。"),
            ReviewType = Required(command.ReviewType, 50, nameof(command.ReviewType), "审查类型不能超过 50 个字符。"),
            ReviewContent = OptionalJson(command.ReviewContent, "审查内容必须是有效 JSON。"),
            ReviewDescription = Optional(command.ReviewDescription, 1000, nameof(command.ReviewDescription), "审查描述不能超过 1000 个字符。"),
            EntityType = Optional(command.EntityType, 100, nameof(command.EntityType), "业务实体类型不能超过 100 个字符。"),
            EntityId = Optional(command.EntityId, 100, nameof(command.EntityId), "业务实体 ID 不能超过 100 个字符。"),
            BusinessData = OptionalJson(command.BusinessData, "业务数据必须是有效 JSON。"),
            ReviewStatus = reviewStatus,
            ReviewResult = NormalizeReviewResult(command.ReviewResult, reviewStatus),
            Priority = command.Priority,
            SubmitUserId = command.SubmitUserId ?? command.CurrentUserId,
            SubmitTime = command.SubmitTime ?? now,
            CurrentReviewUserId = command.CurrentReviewUserId,
            ReviewUserIds = OptionalJson(command.ReviewUserIds, "审查人 ID 列表必须是有效 JSON。"),
            ReviewLevel = command.ReviewLevel,
            CurrentLevel = command.CurrentLevel,
            ReviewStartTime = command.ReviewStartTime,
            ReviewEndTime = ResolveReviewEndTime(command.ReviewEndTime, reviewStatus, now),
            Attachments = OptionalJson(command.Attachments, "附件信息必须是有效 JSON。"),
            ExtendData = OptionalJson(command.ExtendData, "扩展数据必须是有效 JSON。"),
            Status = command.Status,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        return new ReviewCommandResult(await _reviewRepository.AddAsync(review, cancellationToken));
    }

    /// <inheritdoc />
    public async Task DeleteReviewAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var review = await GetReviewOrThrowAsync(id, cancellationToken);
        if (!await _reviewRepository.DeleteAsync(review, cancellationToken))
        {
            throw new InvalidOperationException("系统审查删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<ReviewCommandResult> UpdateReviewAsync(ReviewUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);
        var review = await GetReviewOrThrowAsync(command.BasicId, cancellationToken);
        if (review.ReviewStatus is AuditStatus.Approved or AuditStatus.Rejected or AuditStatus.Withdrawn)
        {
            throw new InvalidOperationException("已完结审查不能修改。");
        }

        review.ReviewTitle = Required(command.ReviewTitle, 200, nameof(command.ReviewTitle), "审查标题不能超过 200 个字符。");
        review.ReviewType = Required(command.ReviewType, 50, nameof(command.ReviewType), "审查类型不能超过 50 个字符。");
        review.ReviewContent = OptionalJson(command.ReviewContent, "审查内容必须是有效 JSON。");
        review.ReviewDescription = Optional(command.ReviewDescription, 1000, nameof(command.ReviewDescription), "审查描述不能超过 1000 个字符。");
        review.EntityType = Optional(command.EntityType, 100, nameof(command.EntityType), "业务实体类型不能超过 100 个字符。");
        review.EntityId = Optional(command.EntityId, 100, nameof(command.EntityId), "业务实体 ID 不能超过 100 个字符。");
        review.BusinessData = OptionalJson(command.BusinessData, "业务数据必须是有效 JSON。");
        review.Priority = command.Priority;
        review.SubmitUserId = command.SubmitUserId;
        review.SubmitTime = command.SubmitTime ?? review.SubmitTime;
        review.CurrentReviewUserId = command.CurrentReviewUserId;
        review.ReviewUserIds = OptionalJson(command.ReviewUserIds, "审查人 ID 列表必须是有效 JSON。");
        review.ReviewLevel = command.ReviewLevel;
        review.CurrentLevel = command.CurrentLevel;
        review.ReviewStartTime = command.ReviewStartTime;
        review.ReviewEndTime = command.ReviewEndTime;
        review.Attachments = OptionalJson(command.Attachments, "附件信息必须是有效 JSON。");
        review.ExtendData = OptionalJson(command.ExtendData, "扩展数据必须是有效 JSON。");
        review.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        return new ReviewCommandResult(await _reviewRepository.UpdateAsync(review, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ReviewCommandResult> UpdateReviewStatusAsync(ReviewStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统审查主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));

        var review = await GetReviewOrThrowAsync(command.BasicId, cancellationToken);
        review.Status = command.Status;
        review.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? review.Remark;

        return new ReviewCommandResult(await _reviewRepository.UpdateAsync(review, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ReviewCommandResult> WithdrawReviewAsync(ReviewWithdrawCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统审查主键必须大于 0。");
        var review = await GetReviewOrThrowAsync(command.BasicId, cancellationToken);
        if (review.ReviewStatus is AuditStatus.Approved or AuditStatus.Rejected or AuditStatus.Withdrawn)
        {
            throw new InvalidOperationException("已完结审查不能撤回。");
        }

        review.ReviewStatus = AuditStatus.Withdrawn;
        review.ReviewEndTime = command.WithdrawTime ?? DateTimeOffset.UtcNow;
        review.CurrentReviewUserId = null;
        review.Remark = Optional(command.Reason, 500, nameof(command.Reason), "撤回原因不能超过 500 个字符。") ?? review.Remark;

        return new ReviewCommandResult(await _reviewRepository.UpdateAsync(review, cancellationToken));
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
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

    private static long? ResolveNextReviewUserId(SysReview review, ReviewAuditCommand command)
    {
        return command.ReviewResult switch
        {
            AuditResult.Pass when review.CurrentLevel < review.ReviewLevel => command.NextReviewUserId ?? review.CurrentReviewUserId,
            AuditResult.Pass => null,
            AuditResult.Reject => null,
            AuditResult.Return => command.NextReviewUserId ?? review.SubmitUserId ?? review.CurrentReviewUserId,
            _ => review.CurrentReviewUserId
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

    private static void ValidateAuditCommand(ReviewAuditCommand command)
    {
        EnsureId(command.BasicId, "系统审查主键必须大于 0。");
        EnsureEnum(command.ReviewResult, nameof(command.ReviewResult));
        EnsureOptionalId(command.ReviewUserId, nameof(command.ReviewUserId), "审查人主键必须大于 0。");
        EnsureOptionalId(command.NextReviewUserId, nameof(command.NextReviewUserId), "下一审查人主键必须大于 0。");
        _ = Optional(command.ReviewComment, 1000, nameof(command.ReviewComment), "审查意见不能超过 1000 个字符。");
        _ = Optional(command.ReviewAction, 50, nameof(command.ReviewAction), "审查动作不能超过 50 个字符。");
        _ = OptionalJson(command.Attachments, "审查日志附件信息必须是有效 JSON。");
        _ = OptionalJson(command.ExtendData, "审查日志扩展数据必须是有效 JSON。");
        _ = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");
    }

    private static void ValidateCreateCommand(ReviewCreateCommand command)
    {
        ValidateReviewInput(
            command.ReviewStatus,
            command.ReviewResult,
            command.SubmitUserId,
            command.CurrentReviewUserId,
            command.ReviewLevel,
            command.CurrentLevel,
            command.ReviewStartTime,
            command.ReviewEndTime,
            command.Status);
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

    private static void ValidateUpdateCommand(ReviewUpdateCommand command)
    {
        EnsureId(command.BasicId, "系统审查主键必须大于 0。");
        ValidateReviewInput(
            null,
            null,
            command.SubmitUserId,
            command.CurrentReviewUserId,
            command.ReviewLevel,
            command.CurrentLevel,
            command.ReviewStartTime,
            command.ReviewEndTime,
            null);
    }

    private static void EnsureEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static void EnsureOptionalId(long? id, string paramName, string message)
    {
        if (id is <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string? OptionalJson(string? value, string message)
    {
        var normalized = NormalizeNullable(value);
        if (normalized is null)
        {
            return null;
        }

        try
        {
            using var _ = JsonDocument.Parse(normalized);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(message, exception);
        }

        return normalized;
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private async Task AddReviewLogAsync(
                                    SysReview review,
        ReviewAuditCommand command,
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
            ReviewUserId = command.ReviewUserId ?? command.CurrentUserId,
            OriginalStatus = originalStatus,
            NewStatus = newStatus,
            ReviewResult = command.ReviewResult,
            ReviewComment = Optional(command.ReviewComment, 1000, nameof(command.ReviewComment), "审查意见不能超过 1000 个字符。"),
            ReviewAction = Optional(command.ReviewAction, 50, nameof(command.ReviewAction), "审查动作不能超过 50 个字符。")
                ?? ResolveDefaultReviewAction(command.ReviewResult),
            Attachments = OptionalJson(command.Attachments, "审查日志附件信息必须是有效 JSON。"),
            ReviewTime = reviewTime,
            ExtendData = OptionalJson(command.ExtendData, "审查日志扩展数据必须是有效 JSON。"),
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        await _reviewLogRepository.AddLogAsync(reviewLog, cancellationToken);
    }

    private async Task<SysReview> GetReviewOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统审查主键必须大于 0。");
        return await _reviewRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统审查不存在。");
    }
}
