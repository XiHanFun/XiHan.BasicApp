// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts;

/// <summary>
/// 跨包的消息主题
/// </summary>
/// <remarks>
/// 格式 <c>basicapp.&lt;包&gt;.&lt;事件&gt;</c>（方案 D7）。领域事件由发出的包经 <c>AddDomainEvent</c> 登记、随业务写入进发件箱，
/// 订阅的包以 <c>AddMessageHandler</c> 订阅（竞争消费，按消息 ID 幂等）；载荷是 Contracts 里的记录。包内自用的主题（如 Realtime 的断开连接广播）不在这里
/// </remarks>
public static class Topics
{
    /// <summary>
    /// 用户的部门归属变了（加入或移出部门）
    /// </summary>
    /// <remarks>
    /// Organization 发出，载荷 <see cref="Organization.MembershipChanged"/>；Chat 据此同步部门群成员
    /// </remarks>
    public const string OrganizationMembershipChanged = "basicapp.organization.membership-changed";

    /// <summary>
    /// 用户的在线状态变了（在某一侧上线或离线）
    /// </summary>
    /// <remarks>
    /// Realtime 在连接登记变化时直接发布（没有业务写入，不经发件箱），载荷 <see cref="Realtime.PresenceChanged"/>；Chat 据此把在线状态推给相关的人
    /// </remarks>
    public const string RealtimePresenceChanged = "basicapp.realtime.presence-changed";
}
