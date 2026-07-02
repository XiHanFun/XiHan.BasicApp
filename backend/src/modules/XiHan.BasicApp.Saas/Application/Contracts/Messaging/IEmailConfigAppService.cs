#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IEmailConfigAppService
// Guid:0e8b3f52-7d24-4a91-b6c8-4f1a9e5d2c73
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 邮件配置命令应用服务接口
/// </summary>
public interface IEmailConfigAppService : IApplicationService
{
    /// <summary>
    /// 创建邮件配置
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>邮件配置详情</returns>
    Task<EmailConfigDetailDto> CreateEmailConfigAsync(EmailConfigCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新邮件配置
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>邮件配置详情</returns>
    Task<EmailConfigDetailDto> UpdateEmailConfigAsync(EmailConfigUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新邮件配置启停状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>邮件配置详情</returns>
    Task<EmailConfigDetailDto> UpdateEmailConfigStatusAsync(EmailConfigStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置默认邮件配置
    /// </summary>
    /// <param name="input">默认更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>邮件配置详情</returns>
    Task<EmailConfigDetailDto> SetDefaultEmailConfigAsync(EmailConfigDefaultUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除邮件配置
    /// </summary>
    /// <param name="id">邮件配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteEmailConfigAsync(long id, CancellationToken cancellationToken = default);
}
