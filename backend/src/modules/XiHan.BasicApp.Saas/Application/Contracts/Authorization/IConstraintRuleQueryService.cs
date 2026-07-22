// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 约束规则查询应用服务接口
/// </summary>
public interface IConstraintRuleQueryService : IApplicationService
{
    /// <summary>
    /// 获取约束规则分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>约束规则分页列表</returns>
    Task<PageResultDtoBase<ConstraintRuleListItemDto>> GetConstraintRulePageAsync(ConstraintRulePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取约束规则详情
    /// </summary>
    /// <param name="id">约束规则主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>约束规则详情</returns>
    Task<ConstraintRuleDetailDto?> GetConstraintRuleDetailAsync(long id, CancellationToken cancellationToken = default);
}
