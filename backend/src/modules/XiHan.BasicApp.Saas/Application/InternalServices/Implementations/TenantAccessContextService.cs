using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Security.Extensions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.InternalServices.Implementations;

/// <summary>
/// 租户访问上下文内部服务实现。
/// </summary>
public class TenantAccessContextService(
    ITenantRepository tenantRepository,
    IUserRepository userRepository,
    IUserSessionRepository userSessionRepository,
    ISqlSugarClientResolver clientResolver,
    ICurrentUser currentUser) : ITenantAccessContextService, IScopedDependency
{
    public async Task<long?> ResolveTargetTenantIdAsync(long? targetTenantId, string? targetTenantCode, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(targetTenantCode))
        {
            var tenant = await tenantRepository.GetByTenantCodeAsync(targetTenantCode.Trim(), cancellationToken);
            return NormalizeTenantId(tenant?.BasicId);
        }

        return NormalizeTenantId(targetTenantId);
    }

    public async Task EnsureTenantAccessAsync(long userId, long? tenantId, CancellationToken cancellationToken = default)
    {
        var resolvedTenantId = NormalizeTenantId(tenantId) ?? 1;
        var now = DateTimeOffset.UtcNow;
        var db = clientResolver.GetCurrentClient();

        var membership = await db.Queryable<SysTenantUser>()
            .Where(m => m.UserId == userId && m.TenantId == resolvedTenantId)
            .Where(m => !m.IsDeleted && m.Status == YesOrNo.Yes && m.InviteStatus == TenantMemberInviteStatus.Accepted)
            .Where(m => !m.EffectiveTime.HasValue || m.EffectiveTime <= now)
            .Where(m => !m.ExpirationTime.HasValue || m.ExpirationTime >= now)
            .FirstAsync(cancellationToken);

        if (membership is null)
        {
            throw new UnauthorizedAccessException("当前账号无权进入目标租户");
        }
    }

    public async Task<CurrentUserSessionContext?> GetCurrentContextAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            return null;
        }

        var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken);
        if (user is null || user.Status != YesOrNo.Yes)
        {
            return null;
        }

        var effectiveTenantId = NormalizeTenantId(currentUser.TenantId);
        var sessionId = currentUser.FindSessionId();
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var session = await userSessionRepository.GetBySessionIdAsync(sessionId, effectiveTenantId, cancellationToken);
            if (session is not null && session.UserId == user.BasicId)
            {
                effectiveTenantId = NormalizeTenantId(session.TenantId);
            }
        }

        await EnsureTenantAccessAsync(user.BasicId, effectiveTenantId, cancellationToken);

        return new CurrentUserSessionContext
        {
            User = user,
            CurrentTenantId = effectiveTenantId,
            SessionId = sessionId
        };
    }

    private static long? NormalizeTenantId(long? tenantId)
    {
        return tenantId.HasValue && tenantId.Value > 0 ? tenantId.Value : null;
    }
}
