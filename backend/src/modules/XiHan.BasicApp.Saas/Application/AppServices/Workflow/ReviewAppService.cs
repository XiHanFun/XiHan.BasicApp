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
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统审查命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统审查")]
public sealed class ReviewAppService
    : SaasApplicationService, IReviewAppService
{
    private readonly ICurrentUser _currentUser;
    private readonly IReviewDomainService _reviewDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ReviewAppService(
        IReviewDomainService reviewDomainService,
        ICurrentUser currentUser)
    {
        _reviewDomainService = reviewDomainService;
        _currentUser = currentUser;
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

        var result = await _reviewDomainService.AuditReviewAsync(ToAuditCommand(input), cancellationToken);
        return ReviewApplicationMapper.ToDetailDto(result.Review);
    }

    /// <summary>
    /// 创建系统审查
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Review.Create)]
    public async Task<ReviewDetailDto> CreateReviewAsync(ReviewCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _reviewDomainService.CreateReviewAsync(ToCreateCommand(input), cancellationToken);
        return ReviewApplicationMapper.ToDetailDto(result.Review);
    }

    /// <summary>
    /// 删除系统审查
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Review.Delete)]
    public async Task DeleteReviewAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _reviewDomainService.DeleteReviewAsync(id, cancellationToken);
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

        var result = await _reviewDomainService.UpdateReviewAsync(ToUpdateCommand(input), cancellationToken);
        return ReviewApplicationMapper.ToDetailDto(result.Review);
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

        var result = await _reviewDomainService.UpdateReviewStatusAsync(
            new ReviewStatusChangeCommand(input.BasicId, input.Status, input.Remark),
            cancellationToken);
        return ReviewApplicationMapper.ToDetailDto(result.Review);
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

        var result = await _reviewDomainService.WithdrawReviewAsync(
            new ReviewWithdrawCommand(input.BasicId, input.Reason, input.WithdrawTime),
            cancellationToken);
        return ReviewApplicationMapper.ToDetailDto(result.Review);
    }

    private ReviewAuditCommand ToAuditCommand(ReviewAuditDto input)
    {
        return new ReviewAuditCommand(
            input.BasicId,
            input.ReviewResult,
            input.ReviewUserId,
            input.NextReviewUserId,
            input.ReviewComment,
            input.ReviewAction,
            input.ReviewTime,
            input.Attachments,
            input.ExtendData,
            input.Remark,
            _currentUser.UserId);
    }

    private ReviewCreateCommand ToCreateCommand(ReviewCreateDto input)
    {
        return new ReviewCreateCommand(
            input.ReviewCode,
            input.ReviewTitle,
            input.ReviewType,
            input.ReviewContent,
            input.ReviewDescription,
            input.EntityType,
            input.EntityId,
            input.BusinessData,
            input.ReviewStatus,
            input.ReviewResult,
            input.Priority,
            input.SubmitUserId,
            input.SubmitTime,
            input.CurrentReviewUserId,
            input.ReviewUserIds,
            input.ReviewLevel,
            input.CurrentLevel,
            input.ReviewStartTime,
            input.ReviewEndTime,
            input.Attachments,
            input.ExtendData,
            input.Status,
            input.Remark,
            _currentUser.UserId);
    }

    private static ReviewUpdateCommand ToUpdateCommand(ReviewUpdateDto input)
    {
        return new ReviewUpdateCommand(
            input.BasicId,
            input.ReviewTitle,
            input.ReviewType,
            input.ReviewContent,
            input.ReviewDescription,
            input.EntityType,
            input.EntityId,
            input.BusinessData,
            input.Priority,
            input.SubmitUserId,
            input.SubmitTime,
            input.CurrentReviewUserId,
            input.ReviewUserIds,
            input.ReviewLevel,
            input.CurrentLevel,
            input.ReviewStartTime,
            input.ReviewEndTime,
            input.Attachments,
            input.ExtendData,
            input.Remark);
    }
}
