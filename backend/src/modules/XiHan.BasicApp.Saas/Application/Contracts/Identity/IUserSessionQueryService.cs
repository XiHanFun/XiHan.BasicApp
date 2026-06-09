#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserSessionQueryService
// Guid:6a39c358-6035-43d7-ae15-fdb1e7d32626
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
/// 用户会话查询应用服务接口
/// </summary>
public interface IUserSessionQueryService : IApplicationService
{
    /// <summary>
    /// 获取用户会话分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户会话分页列表</returns>
    Task<PageResultDtoBase<UserSessionListItemDto>> GetUserSessionPageAsync(UserSessionPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户会话详情
    /// </summary>
    /// <param name="id">会话主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户会话详情</returns>
    Task<UserSessionDetailDto?> GetUserSessionDetailAsync(long id, CancellationToken cancellationToken = default);
}
