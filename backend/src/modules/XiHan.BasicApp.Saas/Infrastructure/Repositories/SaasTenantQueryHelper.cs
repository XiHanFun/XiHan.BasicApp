using SqlSugar;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// SaaS 仓储多租户过滤辅助器。
/// </summary>
internal static class SaasTenantQueryHelper
{
    /// <summary>
    /// 平台租户占位。
    /// </summary>
    public const long PlatformTenantId = 0;

    /// <summary>
    /// 为多租户查询应用租户过滤。
    /// </summary>
    public static ISugarQueryable<TEntity> ApplyTenantFilter<TEntity>(
        ISugarQueryable<TEntity> query,
        long? tenantId,
        bool includePlatform = false)
        where TEntity : class, IMultiTenantEntity, new()
    {
        if (tenantId.HasValue)
        {
            return includePlatform
                ? query.Where(entity => entity.TenantId == tenantId.Value || entity.TenantId == PlatformTenantId)
                : query.Where(entity => entity.TenantId == tenantId.Value);
        }

        return query.Where(entity => entity.TenantId == PlatformTenantId);
    }

    /// <summary>
    /// 统一写入租户ID 解析。
    /// </summary>
    public static long ResolveWriteTenantId(long? tenantId)
    {
        return tenantId ?? PlatformTenantId;
    }
}
