using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Entities;

namespace XiHan.BasicApp.Rbac.Domain.DomainServices;

/// <summary>
/// 租户领域管理器
/// </summary>
public interface ITenantManager
{
    /// <summary>
    /// 创建租户
    /// </summary>
    Task<SysTenant> CreateAsync(SysTenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验租户编码唯一性
    /// </summary>
    Task EnsureTenantCodeUniqueAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改租户状态
    /// </summary>
    Task ChangeStatusAsync(SysTenant tenant, TenantStatus status, CancellationToken cancellationToken = default);
}
