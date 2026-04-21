using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.InternalServices;

/// <summary>
/// RBAC 授权变更通知器。
/// </summary>
public interface IRbacChangeNotifier
{
    /// <summary>
    /// 发布授权变更通知。
    /// </summary>
    Task NotifyAsync(long? tenantId, AuthorizationChangeType changeType);
}
