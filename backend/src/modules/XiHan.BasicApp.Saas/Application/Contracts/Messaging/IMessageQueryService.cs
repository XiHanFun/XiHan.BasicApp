#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMessageQueryService
// Guid:516f56b0-e360-4975-80a6-40c7aabe3e67
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
/// 系统消息查询应用服务接口
/// </summary>
public interface IMessageQueryService : IApplicationService
{
    /// <summary>
    /// 获取系统邮件分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统邮件分页列表</returns>
    Task<PageResultDtoBase<EmailListItemDto>> GetEmailPageAsync(EmailPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统邮件详情
    /// </summary>
    /// <param name="id">系统邮件主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统邮件详情</returns>
    Task<EmailDetailDto?> GetEmailDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统短信分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统短信分页列表</returns>
    Task<PageResultDtoBase<SmsListItemDto>> GetSmsPageAsync(SmsPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统短信详情
    /// </summary>
    /// <param name="id">系统短信主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统短信详情</returns>
    Task<SmsDetailDto?> GetSmsDetailAsync(long id, CancellationToken cancellationToken = default);
}
