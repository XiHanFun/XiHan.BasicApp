#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldMasker
// Guid:e4a5678b-2436-4273-af01-90b451082ae0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Security.Cryptography;
using System.Text;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 字段脱敏器：按 <see cref="FieldMaskStrategy"/> + MaskPattern 生成脱敏值。
/// MaskPattern 约定：部分脱敏用 "keep:N,M"（保留首 N 尾 M）；固定替换/自定义用作占位符文本。
/// </summary>
public static class FieldMasker
{
    /// <summary>
    /// 计算脱敏后的值。可读且不脱敏返回原值；不可读未指定策略默认隐藏(null)。
    /// </summary>
    public static string? Mask(string? raw, bool isReadable, FieldMaskStrategy strategy, string? pattern)
    {
        if (isReadable && strategy == FieldMaskStrategy.None)
        {
            return raw;
        }

        var effective = !isReadable && strategy == FieldMaskStrategy.None
            ? FieldMaskStrategy.Hidden
            : strategy;

        if (effective == FieldMaskStrategy.Hidden)
        {
            return null;
        }

        if (string.IsNullOrEmpty(raw))
        {
            return raw;
        }

        return effective switch
        {
            FieldMaskStrategy.FullMask => new string('*', raw.Length),
            FieldMaskStrategy.PartialMask => PartialMask(raw, pattern),
            FieldMaskStrategy.Hash => Hash(raw),
            FieldMaskStrategy.Redact => string.IsNullOrWhiteSpace(pattern) ? "[已脱敏]" : pattern,
            FieldMaskStrategy.Custom => string.IsNullOrWhiteSpace(pattern) ? new string('*', raw.Length) : pattern,
            _ => raw,
        };
    }

    private static string PartialMask(string raw, string? pattern)
    {
        var keepStart = 0;
        var keepEnd = 0;
        if (!string.IsNullOrWhiteSpace(pattern) && pattern.StartsWith("keep:", StringComparison.OrdinalIgnoreCase))
        {
            var segments = pattern["keep:".Length..].Split(',', StringSplitOptions.TrimEntries);
            if (segments.Length > 0)
            {
                _ = int.TryParse(segments[0], out keepStart);
            }
            if (segments.Length > 1)
            {
                _ = int.TryParse(segments[1], out keepEnd);
            }
        }
        else
        {
            keepEnd = Math.Min(4, raw.Length);
        }

        keepStart = Math.Max(0, keepStart);
        keepEnd = Math.Max(0, keepEnd);
        if (keepStart + keepEnd >= raw.Length)
        {
            return new string('*', raw.Length);
        }

        var middle = new string('*', raw.Length - keepStart - keepEnd);
        return raw[..keepStart] + middle + raw[(raw.Length - keepEnd)..];
    }

    private static string Hash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
