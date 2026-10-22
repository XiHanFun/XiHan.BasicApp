// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 登录时的访问策略检查：核对凭据之后、签发令牌之前，按目标租户与这个用户求一次
/// </summary>
public interface IAccessPolicyLoginGuard
{
    /// <summary>
    /// 不允许这个用户此刻登录进目标租户时，写一条登录失败日志并抛出 <see cref="InvalidOperationException"/>
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="userName">用户名</param>
    /// <param name="tenantId">目标租户；平台为空</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnsureLoginAllowedAsync(long userId, string? userName, long? tenantId, CancellationToken cancellationToken = default);
}
