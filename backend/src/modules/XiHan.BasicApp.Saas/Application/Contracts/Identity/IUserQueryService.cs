#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserQueryService
// Guid:63bd2938-1776-4817-93f5-8f5d869bb098
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户查询应用服务接口
/// </summary>
public interface IUserQueryService : IApplicationService
{
    /// <summary>
    /// 获取用户分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户分页列表</returns>
    Task<PageResultDtoBase<UserListItemDto>> GetUserPageAsync(UserPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户详情
    /// </summary>
    /// <param name="id">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    Task<UserDetailDto?> GetUserDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取已启用用户选择项
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已启用用户选择项</returns>
    Task<IReadOnlyList<UserSelectItemDto>> GetEnabledUsersAsync(UserSelectQueryDto input, CancellationToken cancellationToken = default);
}
