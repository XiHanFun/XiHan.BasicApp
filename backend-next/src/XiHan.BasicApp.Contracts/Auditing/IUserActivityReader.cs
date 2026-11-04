// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Auditing;

/// <summary>
/// 一个用户在一段时间里的活动
/// </summary>
/// <param name="Logins">成功的登录次数</param>
/// <param name="Accesses">访问（接口调用）次数</param>
/// <param name="OnlineSeconds">在线时长（秒，按会话算）</param>
public sealed record UserActivityCounts(int Logins, int Accesses, long OnlineSeconds);

/// <summary>
/// 管理员看别人的活动（Auditing 实现）
/// </summary>
/// <remarks>
/// 只算调用所在一侧的日志与会话（与日志页同一口径）：外部成员在别的租户的活动不算。不做权限与数据范围判定，调用方自己保证看得到这个人
/// </remarks>
public interface IUserActivityReader
{
    /// <summary>
    /// 取用户今天的活动
    /// </summary>
    /// <param name="context">调用上下文</param>
    /// <param name="userId">用户 ID</param>
    /// <param name="zone">按哪个时区分天</param>
    /// <returns>今天的次数与在线时长</returns>
    Task<UserActivityCounts> TodayAsync(InvocationContext context, Guid userId, TimeZoneInfo zone);
}
