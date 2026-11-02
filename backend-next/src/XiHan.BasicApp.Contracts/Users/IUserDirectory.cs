// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Users;

/// <summary>
/// 列表里显示用户、按渠道联系用户用的摘要。
/// </summary>
/// <param name="Id">用户 ID。</param>
/// <param name="UserName">登录名。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="Email">邮箱。</param>
/// <param name="PhoneNumber">手机号。</param>
public sealed record UserSummary(Guid Id, string UserName, string DisplayName, string? Email, string? PhoneNumber);

/// <summary>
/// 用户目录（Identity 实现）：别的包按 ID 取用户的登录名与显示名。
/// </summary>
/// <remarks>
/// 用于列表展示（不做权限与数据范围判定，调用方自己保证只取它看得到的那些 ID）。
/// </remarks>
public interface IUserDirectory
{
    /// <summary>
    /// 取一批用户的摘要。
    /// </summary>
    /// <param name="userIds">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按用户 ID 索引的摘要；不存在的不在结果里（已删除的用户照常返回，历史记录要显示名字）。</returns>
    Task<IReadOnlyDictionary<Guid, UserSummary>> FindAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>
    /// 一侧可见的在用账号。
    /// </summary>
    /// <remarks>
    /// 平台一侧是注册在平台的账号，租户一侧是注册在该租户的账号与在有效期内的普通成员（运维支持成员不算）；只算状态为在用、没删除的。
    /// 用于把候选人收窄到这一侧能收到待办、能被选中的人。
    /// </remarks>
    /// <param name="side">一侧：平台为 <c>platform</c>，否则是租户 ID。</param>
    /// <param name="userIds">候选用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>候选里可见且在用的那些。</returns>
    Task<IReadOnlySet<Guid>> ActiveInSideAsync(string side, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>
    /// 一侧可见的账号，含停用的。
    /// </summary>
    /// <remarks>
    /// 可见口径同 <see cref="ActiveInSideAsync"/>，但不看状态：停用的照样算，已删除的不算。用于按 ID 念出这一侧已经选过的人（选人回显）。
    /// </remarks>
    /// <param name="side">一侧：平台为 <c>platform</c>，否则是租户 ID。</param>
    /// <param name="userIds">候选用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>候选里这一侧可见的那些。</returns>
    Task<IReadOnlySet<Guid>> InSideAsync(string side, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>
    /// 搜一侧可见的在用账号。
    /// </summary>
    /// <remarks>
    /// 可见口径同 <see cref="ActiveInSideAsync"/>；关键字只匹配登录名与显示名（包含，大小写按库的排序规则），不匹配邮箱、手机号等联系方式；按登录名排序。
    /// </remarks>
    /// <param name="side">一侧：平台为 <c>platform</c>，否则是租户 ID。</param>
    /// <param name="keyword">关键字，空即不筛。</param>
    /// <param name="limit">最多返回几个（1～200）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>匹配的账号摘要。</returns>
    Task<IReadOnlyList<UserSummary>> SearchActiveInSideAsync(string side, string? keyword, int limit, CancellationToken cancellationToken);
}
