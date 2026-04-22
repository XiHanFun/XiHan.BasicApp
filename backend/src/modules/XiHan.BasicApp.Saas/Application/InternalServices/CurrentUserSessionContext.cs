using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.InternalServices;

/// <summary>
/// 当前用户会话上下文。
/// </summary>
public sealed class CurrentUserSessionContext
{
    /// <summary>
    /// 当前用户实体。
    /// </summary>
    public required SysUser User { get; init; }

    /// <summary>
    /// 当前会话生效租户。
    /// </summary>
    public long? CurrentTenantId { get; init; }

    /// <summary>
    /// 当前会话标识。
    /// </summary>
    public string? SessionId { get; init; }
}
