// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 判定发起人能否模仿目标用户
/// </summary>
/// <remarks>
/// 由 Access 包实现（要比较授予）、Identity 在 <c>impersonation.start</c> 消费；没有实现时不比较
/// </remarks>
public interface IImpersonationGuard
{
    /// <summary>
    /// 目标在目标上下文的权限有没有超出发起人
    /// </summary>
    /// <param name="actor">发起人的安全上下文</param>
    /// <param name="targetId">目标用户</param>
    /// <param name="tenantId">目标上下文的租户（<see langword="null"/> 即平台侧）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可以模仿时为 <see langword="true"/></returns>
    Task<Result<bool>> AllowsAsync(SecurityContext actor, Guid targetId, string? tenantId, CancellationToken cancellationToken);
}
