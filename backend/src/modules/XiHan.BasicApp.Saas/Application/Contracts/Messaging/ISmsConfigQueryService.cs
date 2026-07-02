#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISmsConfigQueryService
// Guid:3c8f1e50-7a94-4d26-8b03-9e5a2f7d1c48
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 短信配置查询应用服务接口
/// </summary>
public interface ISmsConfigQueryService : IApplicationService
{
    /// <summary>
    /// 获取短信配置分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>短信配置分页列表</returns>
    Task<PageResultDtoBase<SmsConfigListItemDto>> GetSmsConfigPageAsync(SmsConfigPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取短信配置详情
    /// </summary>
    /// <param name="id">短信配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>短信配置详情</returns>
    Task<SmsConfigDetailDto?> GetSmsConfigDetailAsync(long id, CancellationToken cancellationToken = default);
}
