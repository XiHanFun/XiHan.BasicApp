// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 登录时的访问策略：核对凭据之后、签发令牌之前，按目标租户与这个用户求一次；登录失败按 IP 计入自动封禁
/// </summary>
public interface IAccessPolicyLoginGuard
{
    /// <summary>
    /// 不允许这个用户此刻登录进目标租户时，写一条登录失败日志、完成当前工作单元让它落库，再抛出 <see cref="InvalidOperationException"/>
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="userName">用户名</param>
    /// <param name="tenantId">目标租户；平台为空</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnsureLoginAllowedAsync(long userId, string? userName, long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 一次登录失败（凭据、登录验证码或两步验证码不对）按客户端 IP 计入自动封禁；IP 为空不计，计数出错只记日志
    /// </summary>
    /// <param name="ipAddress">客户端 IP</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task CountFailedLoginAsync(string? ipAddress, CancellationToken cancellationToken = default);
}
