// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 文本字段脱敏
/// </summary>
public static class FieldMasker
{
    /// <summary>
    /// 按有效规则处理一个文本值；空值原样返回（隐藏除外，一律返回空）
    /// </summary>
    public static string? Mask(string? raw, EffectiveFieldRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (rule.MaskStrategy == FieldMaskStrategy.None)
        {
            return raw;
        }

        if (rule.MaskStrategy == FieldMaskStrategy.Hidden)
        {
            return null;
        }

        if (string.IsNullOrEmpty(raw))
        {
            return raw;
        }

        return rule.MaskStrategy switch
        {
            FieldMaskStrategy.FullMask => new string('*', raw.Length),
            FieldMaskStrategy.PartialMask => PartialMask(raw, rule.MaskKeepHead ?? 0, rule.MaskKeepTail ?? 0),
            FieldMaskStrategy.Hash => Hash(raw),
            FieldMaskStrategy.Redact => rule.MaskReplacement ?? throw new InvalidOperationException($"字段「{rule.FieldName}」的固定文本规则缺少文字。"),
            _ => throw new InvalidOperationException($"字段「{rule.FieldName}」的读取方式 {rule.MaskStrategy} 无效。")
        };
    }

    /// <summary>
    /// 保留前 keepHead 位、后 keepTail 位，其余替换为 *；保留位数不小于原长时整体打码，避免原样漏出
    /// </summary>
    private static string PartialMask(string raw, int keepHead, int keepTail)
    {
        if (keepHead + keepTail >= raw.Length)
        {
            return new string('*', raw.Length);
        }

        return string.Concat(raw.AsSpan(0, keepHead), new string('*', raw.Length - keepHead - keepTail), raw.AsSpan(raw.Length - keepTail));
    }

    /// <summary>
    /// SHA-256 前 16 位小写十六进制：相同原值得到相同结果，可用于比对
    /// </summary>
    private static string Hash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
