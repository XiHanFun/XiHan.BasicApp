// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 租户成员查询应用服务接口
/// </summary>
public interface ITenantMemberQueryService : IApplicationService
{
    /// <summary>
    /// 获取租户成员分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员分页列表</returns>
    Task<PageResultDtoBase<TenantMemberListItemDto>> GetTenantMemberPageAsync(TenantMemberPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户成员详情
    /// </summary>
    /// <param name="id">租户成员主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员详情</returns>
    Task<TenantMemberDetailDto?> GetTenantMemberDetailAsync(long id, CancellationToken cancellationToken = default);
}
