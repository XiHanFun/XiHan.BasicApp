// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户安全查询应用服务接口
/// </summary>
public interface IUserSecurityQueryService : IApplicationService
{
    /// <summary>
    /// 获取用户安全分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全分页列表</returns>
    Task<PageResultDtoBase<UserSecurityListItemDto>> GetUserSecurityPageAsync(UserSecurityPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户安全详情
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    Task<UserSecurityDetailDto?> GetUserSecurityDetailAsync(long userId, CancellationToken cancellationToken = default);
}
