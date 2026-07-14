#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SessionLockReasons
// Guid:6d0a4e73-5b18-4c92-a7f6-2e9b1c8d0f35
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/15 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 会话锁定原因
/// </summary>
/// <remarks>
/// 锁定是通用能力，<b>锁屏只是其中一种原因</b>。原因决定客户端引导哪种解锁方式：
/// 锁屏走口令解锁，将来若加入风控挂起则可能要走申诉、强制改密则跳改密页。
/// <para>
/// 用字符串常量而非枚举：这个值要经 423 响应体透传给前端、并被框架原样转发（框架不解释原因），
/// 字符串在跨端契约里比数字枚举更不易错配。
/// </para>
/// </remarks>
public static class SessionLockReasons
{
    /// <summary>
    /// 用户主动锁屏（口令解锁；口令存于 <c>SysUserSession.LockPasswordHash</c>，会话级一次性）
    /// </summary>
    public const string ScreenLock = "ScreenLock";
}
