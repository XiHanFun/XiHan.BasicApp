// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 登录失败自动封禁
/// </summary>
public interface ILoginBanService
{
    /// <summary>
    /// 这个 IP 封禁到什么时候；没被封时为空
    /// </summary>
    Task<DateTimeOffset?> GetBanAsync(string ip, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记一次登录失败，达到阈值即封禁
    /// </summary>
    Task RecordFailureAsync(string ip, CancellationToken cancellationToken = default);

    /// <summary>
    /// 当前的封禁
    /// </summary>
    Task<IReadOnlyList<LoginBanEntry>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 解封并清掉失败计数
    /// </summary>
    /// <returns>原先是否在封禁中</returns>
    Task<bool> UnbanAsync(string ip, CancellationToken cancellationToken = default);
}
