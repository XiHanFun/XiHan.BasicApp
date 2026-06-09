#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConstraintRuleQueryService
// Guid:2a7267f8-bf68-4309-8069-e849e97360f8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
