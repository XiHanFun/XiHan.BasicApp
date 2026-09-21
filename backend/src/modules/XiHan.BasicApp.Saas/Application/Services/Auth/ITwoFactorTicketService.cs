// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 两步验证票据服务：密码登录通过图形验证码与密码校验、进入两步验证后签发的短时凭据
/// </summary>
/// <remarks>
/// 两步验证是无状态三段式（凭据 → 选方式 / 下发码 → 提交码），每段都重新提交同一个登录请求；
/// 图形验证码消费即销毁，第二段起无法再次校验。票据代表「本次登录已通过图形验证码」这一事实，
/// 在有效期内可多次出示，直到登录完成作废或过期。票据只替代图形验证码，不替代密码，每段仍须重新认证密码。
/// 票据同时绑定用户主键与首段提交的登录名：登录名在密码认证之前比对，令持票人不能借免图形码去试探他人账号；
/// 用户主键在认证之后比对，令票据不能跨用户使用。
/// </remarks>
public interface ITwoFactorTicketService
{
    /// <summary>
    /// 票据有效秒数
    /// </summary>
    int ExpiresInSeconds { get; }

    /// <summary>
    /// 为已通过密码认证、待完成两步验证的用户签发票据
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="login">首段提交并规范化后的登录名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>不透明随机票据</returns>
    Task<string> IssueAsync(long userId, string login, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解析票据绑定的用户主键与登录名；票据不存在、已过期或值损坏返回 null
    /// </summary>
    /// <param name="ticket">票据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>票据绑定的用户主键与登录名</returns>
    Task<TwoFactorTicketPayload?> ResolveAsync(string? ticket, CancellationToken cancellationToken = default);

    /// <summary>
    /// 作废票据（登录完成或票据与本次请求不匹配时调用；票据不存在时静默返回）
    /// </summary>
    /// <param name="ticket">票据</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RevokeAsync(string ticket, CancellationToken cancellationToken = default);
}

/// <summary>
/// 两步验证票据绑定的事实
/// </summary>
/// <param name="UserId">签发时通过密码认证的用户主键</param>
/// <param name="Login">签发时提交并规范化后的登录名</param>
public sealed record TwoFactorTicketPayload(long UserId, string Login);
