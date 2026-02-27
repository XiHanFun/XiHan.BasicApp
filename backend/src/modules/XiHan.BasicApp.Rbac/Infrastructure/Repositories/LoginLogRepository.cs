using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 登录日志仓储实现
/// </summary>
public class LoginLogRepository : SqlSugarRepositoryBase<SysLoginLog, long>, ILoginLogRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    public LoginLogRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取最近失败次数
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="minutes"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> GetRecentFailureCountAsync(string userName, int minutes, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        if (minutes <= 0)
        {
            return 0;
        }

        var since = DateTimeOffset.UtcNow.AddMinutes(-minutes);
        var query = _dbContext.CreateQueryable<SysLoginLog>()
            .Where(log => log.UserName == userName
                          && log.LoginTime >= since
                          && log.LoginResult != LoginResult.Success);

        if (tenantId.HasValue)
        {
            query = query.Where(log => log.TenantId == tenantId.Value);
        }

        var count = await query.CountAsync(cancellationToken);
        return count;
    }
}
