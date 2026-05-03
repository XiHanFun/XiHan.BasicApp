#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAccessLogQueryService
// Guid:a868a5f4-4a17-4d21-bafa-5791903aa9a2
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
/// 访问日志查询应用服务接口
/// </summary>
public interface IAccessLogQueryService : IApplicationService
{
    /// <summary>
    /// 获取访问日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>访问日志分页列表</returns>
    Task<PageResultDtoBase<AccessLogListItemDto>> GetAccessLogPageAsync(AccessLogPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取访问日志详情
    /// </summary>
    /// <param name="id">访问日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>访问日志详情</returns>
    Task<AccessLogDetailDto?> GetAccessLogDetailAsync(long id, CancellationToken cancellationToken = default);
}
