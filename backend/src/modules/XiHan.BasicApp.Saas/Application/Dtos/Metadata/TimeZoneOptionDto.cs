// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos.Metadata;

/// <summary>
/// 时区选项
/// </summary>
public sealed class TimeZoneOptionDto
{
    /// <summary>
    /// IANA 时区标识（保存值）
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 运行环境给出的显示名称（不同操作系统可能不同，仅供展示）
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 相对 UTC 的基础偏移分钟数
    /// </summary>
    public int BaseUtcOffsetMinutes { get; set; }

    /// <summary>
    /// 是否使用夏令时
    /// </summary>
    public bool SupportsDaylightSavingTime { get; set; }
}
