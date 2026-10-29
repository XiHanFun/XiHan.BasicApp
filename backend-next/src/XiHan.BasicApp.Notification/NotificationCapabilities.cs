// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Notification;

/// <summary>
/// 通知包提供的能力。
/// </summary>
public static class NotificationCapabilities
{
    /// <summary>
    /// 通知能力：通知公告与收件箱。
    /// </summary>
    public const string Notification = AppInfo.CapabilityPrefix + "notification";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = 1;
}
