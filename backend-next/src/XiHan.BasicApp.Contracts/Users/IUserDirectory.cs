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
/// 用户目录（Identity 实现）：别的包按 ID 取用户的登录名与显示名，用于列表展示（不做权限与数据范围判定，调用方自己保证只取它看得到的那些 ID）。
/// </summary>
public interface IUserDirectory
{
    /// <summary>
    /// 一批用户的摘要；不存在的不在结果里（已删除的用户照常返回，历史记录要显示名字）。
    /// </summary>
    /// <param name="userIds">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按用户 ID 索引的摘要。</returns>
    Task<IReadOnlyDictionary<Guid, UserSummary>> FindAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
