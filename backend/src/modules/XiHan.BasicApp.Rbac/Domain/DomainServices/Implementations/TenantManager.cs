using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;

namespace XiHan.BasicApp.Rbac.Domain.DomainServices.Implementations;

/// <summary>
/// 租户领域管理器实现
/// </summary>
public class TenantManager : ITenantManager
{
    private readonly ITenantRepository _tenantRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tenantRepository">租户仓储</param>
    public TenantManager(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    /// <summary>
    /// 创建租户
    /// </summary>
    /// <param name="tenant">租户实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的租户实体</returns>
    public async Task<SysTenant> CreateAsync(SysTenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        await EnsureTenantCodeUniqueAsync(tenant.TenantCode, null, cancellationToken);

        tenant.Enable();
        tenant.ChangeTenantStatus(TenantStatus.Normal);
        return await _tenantRepository.AddAsync(tenant, cancellationToken);
    }

    /// <summary>
    /// 校验租户编码唯一性
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="excludeTenantId">排除的租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否唯一</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task EnsureTenantCodeUniqueAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);
        var exists = await _tenantRepository.IsTenantCodeExistsAsync(tenantCode, excludeTenantId, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"租户编码 '{tenantCode}' 已存在");
        }
    }

    /// <summary>
    /// 修改租户状态
    /// </summary>
    /// <param name="tenant">租户实体</param>
    /// <param name="status">租户状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task ChangeStatusAsync(SysTenant tenant, TenantStatus status, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (status == TenantStatus.Disabled)
        {
            tenant.Disable();
        }
        else
        {
            tenant.Enable();
            tenant.ChangeTenantStatus(status);
        }

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
    }
}
