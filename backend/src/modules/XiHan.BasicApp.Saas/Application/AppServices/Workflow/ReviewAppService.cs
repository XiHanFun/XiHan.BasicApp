// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

        var result = await _reviewDomainService.AuditReviewAsync(
            ReviewApplicationMapper.ToAuditCommand(input, _currentUser.UserId),
            cancellationToken);
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

        var result = await _reviewDomainService.CreateReviewAsync(
            ReviewApplicationMapper.ToCreateCommand(input, _currentUser.UserId),
            cancellationToken);
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

        var result = await _reviewDomainService.UpdateReviewAsync(ReviewApplicationMapper.ToUpdateCommand(input), cancellationToken);
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

        var result = await _reviewDomainService.UpdateReviewStatusAsync(ReviewApplicationMapper.ToStatusCommand(input), cancellationToken);
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

        var result = await _reviewDomainService.WithdrawReviewAsync(ReviewApplicationMapper.ToWithdrawCommand(input), cancellationToken);
        return ReviewApplicationMapper.ToDetailDto(result.Review);
    }
}
