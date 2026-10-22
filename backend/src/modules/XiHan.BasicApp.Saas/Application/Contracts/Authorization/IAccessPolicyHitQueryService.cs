// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 条件命中记录查询应用服务接口
/// </summary>
public interface IAccessPolicyHitQueryService : IApplicationService
{
    /// <summary>
    /// 命中记录分页列表（当前层）
    /// </summary>
    Task<PageResultDtoBase<AccessPolicyHitListItemDto>> GetAccessPolicyHitPageAsync(AccessPolicyHitPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 命中记录详情（当前层）
    /// </summary>
    Task<AccessPolicyHitListItemDto?> GetAccessPolicyHitDetailAsync(long id, CancellationToken cancellationToken = default);
}
