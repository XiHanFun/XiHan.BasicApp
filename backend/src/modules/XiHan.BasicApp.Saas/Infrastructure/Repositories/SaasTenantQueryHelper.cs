using SqlSugar;
using System.Linq.Expressions;

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
        where TEntity : class, new()
    {
        ArgumentNullException.ThrowIfNull(query);

        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var tenantProperty = Expression.PropertyOrField(parameter, "TenantId");
        var platformTenant = Expression.Constant(PlatformTenantId, tenantProperty.Type);

        Expression predicateBody;
        if (tenantId.HasValue)
        {
            var tenantValue = Expression.Constant(Convert.ChangeType(tenantId.Value, tenantProperty.Type), tenantProperty.Type);
            predicateBody = includePlatform
                ? Expression.OrElse(
                    Expression.Equal(tenantProperty, tenantValue),
                    Expression.Equal(tenantProperty, platformTenant))
                : Expression.Equal(tenantProperty, tenantValue);
        }
        else
        {
            predicateBody = Expression.Equal(tenantProperty, platformTenant);
        }

        var lambda = Expression.Lambda<Func<TEntity, bool>>(predicateBody, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// 统一写入租户ID 解析。
    /// </summary>
    public static long ResolveWriteTenantId(long? tenantId)
    {
        return tenantId ?? PlatformTenantId;
    }
}
