#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserSecurityQueryService
// Guid:8f513a98-39d5-4fa1-83a2-1c3f1f903aaf
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
