using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 登录日志仓储接口
/// </summary>
public interface ILoginLogRepository : IRepositoryBase<SysLoginLog, long>
{
    /// <summary>
    /// 获取用户最近失败登录次数
    /// </summary>
    Task<int> GetRecentFailureCountAsync(string userName, int minutes, long? tenantId = null, CancellationToken cancellationToken = default);
}
