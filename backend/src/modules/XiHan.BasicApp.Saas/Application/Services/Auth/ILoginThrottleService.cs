// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 登录节流服务：密码登录的防爆破限流（账号 + IP 双维度固定窗口计数）
/// </summary>
public interface ILoginThrottleService
{
    /// <summary>
    /// 校验指定账号从指定 IP 的密码登录尝试是否在限流窗口内
    /// </summary>
    /// <param name="account">登录账号（用户名或邮箱）</param>
    /// <param name="ipAddress">客户端 IP 地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">超出窗口允许的尝试次数</exception>
    Task EnsureLoginAllowedAsync(string account, string? ipAddress, CancellationToken cancellationToken = default);
}
