// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Realtime;

/// <summary>
/// 用户的在线状态变了
/// </summary>
/// <remarks>
/// 主题 <see cref="Topics.RealtimePresenceChanged"/> 的载荷，信封带用户所在的侧
/// 投递至少一次、可能乱序，所以不带「上线还是离线」：订阅方经 <see cref="IRealtimePresence"/> 读处理时的状态
/// </remarks>
/// <param name="UserId">用户</param>
public sealed record PresenceChanged(Guid UserId);
