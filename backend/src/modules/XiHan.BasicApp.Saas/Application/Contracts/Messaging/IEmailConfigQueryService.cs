// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 邮件配置查询应用服务接口
/// </summary>
public interface IEmailConfigQueryService : IApplicationService
{
    /// <summary>
    /// 获取邮件配置分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>邮件配置分页列表</returns>
    Task<PageResultDtoBase<EmailConfigListItemDto>> GetEmailConfigPageAsync(EmailConfigPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取邮件配置详情
    /// </summary>
    /// <param name="id">邮件配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>邮件配置详情</returns>
    Task<EmailConfigDetailDto?> GetEmailConfigDetailAsync(long id, CancellationToken cancellationToken = default);
}
