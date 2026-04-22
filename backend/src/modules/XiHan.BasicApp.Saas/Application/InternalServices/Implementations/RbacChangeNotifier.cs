using XiHan.BasicApp.Saas.Application.Caching.Events;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.InternalServices.Implementations;

/// <summary>
/// RBAC 授权变更通知器。
/// </summary>
public class RbacChangeNotifier : IRbacChangeNotifier, IScopedDependency
{
    private readonly ILocalEventBus _localEventBus;

    public RbacChangeNotifier(ILocalEventBus localEventBus)
    {
        _localEventBus = localEventBus;
    }

    public Task NotifyAsync(long? tenantId, AuthorizationChangeType changeType)
    {
        return _localEventBus.PublishAsync(new RbacAuthorizationChangedEvent(tenantId, changeType));
    }
}
