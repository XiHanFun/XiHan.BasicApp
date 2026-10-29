// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Settings.Domain.Entities;

/// <summary>
/// 发号的永久记录（平台库实体，只追加）。
/// </summary>
/// <remarks>
/// 兼作幂等事实与审计。<see cref="OwnerTenantId"/> 是规则所属一侧，<see cref="RequestTenantId"/> 是发起调用的一侧，两者在租户共用平台规则时不同。
/// 号码按格式快照与流水区间重建，不存号码列表。
/// </remarks>
public sealed class SysNumberingAllocation : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 规则主键。
    /// </summary>
    public Guid RuleId { get; set; }

    /// <summary>
    /// 规则编码快照。
    /// </summary>
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>
    /// 发起调用的一侧：租户 ID，平台为 <c>platform</c>。
    /// </summary>
    public string RequestTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 调用方给的幂等键：同一侧对同一规则用同一个键只发一次号。
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// 请求参数的 SHA-256 指纹（十六进制）：同键异参即冲突。
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>
    /// 本次发出的个数（1 到 1000）。
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 起始流水值（含）。
    /// </summary>
    public long StartValue { get; set; }

    /// <summary>
    /// 结束流水值（含）。
    /// </summary>
    public long EndValue { get; set; }

    /// <summary>
    /// 所属周期键。
    /// </summary>
    public string PeriodKey { get; set; } = string.Empty;

    /// <summary>
    /// 前缀快照。
    /// </summary>
    public string? PrefixSnapshot { get; set; }

    /// <summary>
    /// 分隔符快照。
    /// </summary>
    public string SeparatorSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 日期段文本快照。
    /// </summary>
    public string? DateTextSnapshot { get; set; }

    /// <summary>
    /// 流水位数快照。
    /// </summary>
    public int SerialLengthSnapshot { get; set; }

    /// <summary>
    /// 发号时刻（UTC），决定周期与日期段。
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>
    /// 业务类型（调用方自述）。
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务单据主键（调用方自述）。
    /// </summary>
    public string? BusinessId { get; set; }
}
