#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserStatisticsQueryService
// Guid:728fcfcf-073b-4ac0-b7bb-458dba8f18f4
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
/// 用户统计查询应用服务接口
/// </summary>
public interface IUserStatisticsQueryService : IApplicationService
{
    /// <summary>
    /// 获取用户统计分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户统计分页列表</returns>
    Task<PageResultDtoBase<UserStatisticsListItemDto>> GetUserStatisticsPageAsync(UserStatisticsPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户统计详情
    /// </summary>
    /// <param name="id">统计主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户统计详情</returns>
    Task<UserStatisticsDetailDto?> GetUserStatisticsDetailAsync(long id, CancellationToken cancellationToken = default);
}
