namespace XiHan.BasicApp.Saas.Application.InternalServices;

/// <summary>
/// 租户访问上下文内部服务。
/// </summary>
public interface ITenantAccessContextService
{
    /// <summary>
    /// 解析登录目标租户。
    /// </summary>
    Task<long?> ResolveTargetTenantIdAsync(long? targetTenantId, string? targetTenantCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 确保用户在目标租户下具备有效成员身份。
    /// </summary>
    Task EnsureTenantAccessAsync(long userId, long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前会话上下文。
    /// </summary>
    Task<CurrentUserSessionContext?> GetCurrentContextAsync(CancellationToken cancellationToken = default);
}
