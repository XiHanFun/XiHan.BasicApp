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
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>不透明随机票据</returns>
    Task<string> IssueAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解析票据绑定的用户主键；票据不存在或已过期返回 null
    /// </summary>
    /// <param name="ticket">票据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>票据绑定的用户主键</returns>
    Task<long?> ResolveUserIdAsync(string? ticket, CancellationToken cancellationToken = default);

    /// <summary>
    /// 作废票据（登录完成后调用；票据不存在时静默返回）
    /// </summary>
    /// <param name="ticket">票据</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RevokeAsync(string ticket, CancellationToken cancellationToken = default);
}
