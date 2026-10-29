// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Domain.Services;

/// <summary>
/// 调用方：日志归属的一侧与认证后的主体。
/// </summary>
/// <remarks>
/// 日志归属的一侧是当前租户，没有即平台；主体的用户、登录名、会话取访问令牌的声明。
/// </remarks>
/// <param name="TenantId">日志归属的租户；平台一侧为 <see cref="TenantIds.Platform"/>。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="SessionId">会话。</param>
public readonly record struct AuditCaller(string TenantId, Guid? UserId, string? UserName, Guid? SessionId)
{
    /// <summary>
    /// 从调用上下文取调用方：匿名调用只有归属的一侧。
    /// </summary>
    public static AuditCaller From(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var security = context.GetSecurity();
        var tenant = security.Tenant?.Id ?? TenantIds.Platform;
        var principal = security.Principal;
        if (!principal.IsAuthenticated)
        {
            return new AuditCaller(tenant, null, null, null);
        }

        return new AuditCaller(
            tenant,
            Guid.TryParse(principal.Id, out var userId) ? userId : null,
            principal.ClaimValues(IdentityClaims.UserName).FirstOrDefault(),
            Guid.TryParse(principal.ClaimValues(IdentityClaims.Session).FirstOrDefault(), out var sessionId) ? sessionId : null);
    }

    /// <summary>
    /// 把调用方写进一条日志的公共列（不含时间与 trace-id）。
    /// </summary>
    public void Stamp(AuditLogRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        record.TenantId = TenantId;
        record.UserId = UserId;
        record.UserName = UserName;
        record.SessionId = SessionId;
    }
}
