// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 系统审查命令应用服务接口
/// </summary>
public interface IReviewAppService : IApplicationService
{
    /// <summary>
    /// 创建系统审查
    /// </summary>
    Task<ReviewDetailDto> CreateReviewAsync(ReviewCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新系统审查
    /// </summary>
    Task<ReviewDetailDto> UpdateReviewAsync(ReviewUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新系统审查启停状态
    /// </summary>
    Task<ReviewDetailDto> UpdateReviewStatusAsync(ReviewStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 审核系统审查
    /// </summary>
    Task<ReviewDetailDto> AuditReviewAsync(ReviewAuditDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤回系统审查
    /// </summary>
    Task<ReviewDetailDto> WithdrawReviewAsync(ReviewWithdrawDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除系统审查
    /// </summary>
    Task DeleteReviewAsync(long id, CancellationToken cancellationToken = default);
}
