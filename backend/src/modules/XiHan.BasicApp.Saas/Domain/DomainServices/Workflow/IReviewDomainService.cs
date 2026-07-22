// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 审查领域服务
/// </summary>
public interface IReviewDomainService
{
    /// <summary>
    /// 审核审查
    /// </summary>
    Task<ReviewCommandResult> AuditReviewAsync(ReviewAuditCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建审查
    /// </summary>
    Task<ReviewCommandResult> CreateReviewAsync(ReviewCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除审查
    /// </summary>
    Task DeleteReviewAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新审查
    /// </summary>
    Task<ReviewCommandResult> UpdateReviewAsync(ReviewUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新审查启停状态
    /// </summary>
    Task<ReviewCommandResult> UpdateReviewStatusAsync(ReviewStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤回审查
    /// </summary>
    Task<ReviewCommandResult> WithdrawReviewAsync(ReviewWithdrawCommand command, CancellationToken cancellationToken = default);
}
