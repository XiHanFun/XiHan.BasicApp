// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Auditing;

/// <summary>
/// 审计包的能力 ID 与版本。
/// </summary>
public static class AuditingCapabilities
{
    /// <summary>
    /// 审计能力：分表日志的写入与查询、操作日志查询面、链路聚合、保留清理。
    /// </summary>
    public const string Auditing = AppInfo.CapabilityPrefix + "auditing";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = 1;
}
