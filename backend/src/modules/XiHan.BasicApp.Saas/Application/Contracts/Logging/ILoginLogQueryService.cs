#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ILoginLogQueryService
// Guid:c9eb0cb4-d296-43d6-a860-26e071962adf
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
/// 登录日志查询应用服务接口
/// </summary>
public interface ILoginLogQueryService : IApplicationService
{
    /// <summary>
    /// 获取登录日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录日志分页列表</returns>
    Task<PageResultDtoBase<LoginLogListItemDto>> GetLoginLogPageAsync(LoginLogPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取登录日志详情
    /// </summary>
    /// <param name="id">登录日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>登录日志详情</returns>
    Task<LoginLogDetailDto?> GetLoginLogDetailAsync(long id, CancellationToken cancellationToken = default);
}
