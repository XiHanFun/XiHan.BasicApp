// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 短信配置命令应用服务接口
/// </summary>
public interface ISmsConfigAppService : IApplicationService
{
    /// <summary>
    /// 创建短信配置
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>短信配置详情</returns>
    Task<SmsConfigDetailDto> CreateSmsConfigAsync(SmsConfigCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新短信配置
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>短信配置详情</returns>
    Task<SmsConfigDetailDto> UpdateSmsConfigAsync(SmsConfigUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新短信配置启停状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>短信配置详情</returns>
    Task<SmsConfigDetailDto> UpdateSmsConfigStatusAsync(SmsConfigStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置默认短信配置
    /// </summary>
    /// <param name="input">默认更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>短信配置详情</returns>
    Task<SmsConfigDetailDto> SetDefaultSmsConfigAsync(SmsConfigDefaultUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除短信配置
    /// </summary>
    /// <param name="id">短信配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteSmsConfigAsync(long id, CancellationToken cancellationToken = default);
}
