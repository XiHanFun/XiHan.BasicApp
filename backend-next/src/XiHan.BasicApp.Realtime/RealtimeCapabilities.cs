// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Realtime;

/// <summary>
/// 实时包提供的能力。
/// </summary>
public static class RealtimeCapabilities
{
    /// <summary>
    /// 实时能力：通知 Hub、在线状态与推送。
    /// </summary>
    public const string Realtime = AppInfo.CapabilityPrefix + "realtime";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = 1;
}
