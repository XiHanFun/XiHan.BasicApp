// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 权限委托查询应用服务接口
/// </summary>
public interface IPermissionDelegationQueryService : IApplicationService
{
    /// <summary>
    /// 获取权限委托分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限委托分页列表</returns>
    Task<PageResultDtoBase<PermissionDelegationListItemDto>> GetPermissionDelegationPageAsync(PermissionDelegationPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取权限委托详情
    /// </summary>
    /// <param name="id">权限委托主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限委托详情</returns>
    Task<PermissionDelegationDetailDto?> GetPermissionDelegationDetailAsync(long id, CancellationToken cancellationToken = default);
}
