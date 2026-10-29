// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 一次即将签发令牌的登录。
/// </summary>
/// <remarks>
/// 账号凭据都已核对（含两步验证），记着要进入的一侧与此刻的角色。
/// </remarks>
/// <param name="TenantId">要进入的一侧（平台为 <see cref="TenantIds.Platform"/>）。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="RoleIds">在这一侧此刻生效的角色 ID（进令牌的那一份）。</param>
public sealed record LoginAttempt(string TenantId, Guid UserId, string UserName, IReadOnlyList<string> RoleIds);

/// <summary>
/// 登录守卫（Access 实现）。
/// </summary>
/// <remarks>
/// Identity 在登录链路上调用；没有实现即全部放行、不计数。签发令牌前按登录时机的访问策略判定，
/// 认证失败时按调用来源地址计数（登录失败自动封禁）。
/// </remarks>
public interface ILoginGuard
{
    /// <summary>
    /// 开会话、签令牌之前判定这次登录。
    /// </summary>
    /// <remarks>
    /// 用于密码登录、两步验证的第二步、切换租户。
    /// </remarks>
    /// <param name="attempt">这次登录。</param>
    /// <param name="context">调用上下文（来源地址、User-Agent、时刻）。</param>
    /// <returns>判定结果；失败即不签发，错误原样返回给调用方。</returns>
    ValueTask<Result> CheckAsync(LoginAttempt attempt, InvocationContext context);

    /// <summary>
    /// 记一次认证失败。
    /// </summary>
    /// <remarks>
    /// 认证失败指账号不存在、密码错、两步验证码错；图形验证码错与节流拒绝不算。
    /// </remarks>
    /// <param name="context">调用上下文（来源地址）。</param>
    ValueTask RecordFailureAsync(InvocationContext context);
}
