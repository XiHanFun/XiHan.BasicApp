// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 登录失败计数与封禁名单的存储；多实例部署须用 Redis 实现
/// </summary>
public interface ILoginBanStore
{
    /// <summary>
    /// 失败计数加一；首次计数时设窗口过期
    /// </summary>
    /// <returns>窗口内的累计次数</returns>
    Task<long> IncrementFailureAsync(string ip, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>
    /// 24 小时内最近一次封禁的时长
    /// </summary>
    Task<TimeSpan?> GetLastBanDurationAsync(string ip, CancellationToken cancellationToken = default);

    /// <summary>
    /// 封禁到 <paramref name="until"/>，记下这次时长（24 小时），清掉失败计数
    /// </summary>
    Task BanAsync(string ip, DateTimeOffset until, TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 封禁到什么时候；没被封或已到期时为空
    /// </summary>
    Task<DateTimeOffset?> GetBanExpirationAsync(string ip, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 当前的封禁，按到期时间排列
    /// </summary>
    Task<IReadOnlyList<LoginBanEntry>> ListAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解封，并清掉失败计数与封禁历史
    /// </summary>
    /// <returns>原先是否在封禁中</returns>
    Task<bool> UnbanAsync(string ip, DateTimeOffset now, CancellationToken cancellationToken = default);
}
