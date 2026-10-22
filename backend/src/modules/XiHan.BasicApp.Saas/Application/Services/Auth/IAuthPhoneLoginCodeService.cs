// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 手机号验证码登录的验证码下发与校验服务
/// </summary>
/// <remarks>
/// 验证码基于框架 IOneTimeCodeService（分布式缓存后端）；短信走现有的短信网关配置与发件箱，网关没配好时直接报错，不假装已发送。
/// </remarks>
public interface IAuthPhoneLoginCodeService
{
    /// <summary>
    /// 验证码有效期（秒）
    /// </summary>
    int ExpiresInSeconds { get; }

    /// <summary>
    /// 为手机号生成登录验证码并经短信发送
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="receiveUserId">接收账号（发件箱记录归属）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的验证码</returns>
    /// <exception cref="InvalidOperationException">短信网关未配置或未启用</exception>
    Task<string> IssueAndSendAsync(string phone, long receiveUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验并消费手机号的登录验证码（一次性，消费即销毁）
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="code">待校验验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>校验是否通过</returns>
    Task<bool> TryConsumeAsync(string phone, string? code, CancellationToken cancellationToken = default);
}
