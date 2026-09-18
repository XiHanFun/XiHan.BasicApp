// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using PhoneNumbers;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 基于 libphonenumber 的手机号码正规化实现
/// </summary>
/// <remarks>
/// 只认手机（Mobile / FixedLineOrMobile）：登录验证码要发短信，固话收不到。
/// 解析失败与「号码在该国家不成立」一律按格式无效处理，不做静默兜底——
/// 兜底会把打错的号码存进库，用户要到收不到短信才发现。
/// </remarks>
public sealed class PhoneNumberNormalizer : IPhoneNumberNormalizer
{
    private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

    /// <inheritdoc />
    public bool TryNormalize(string? raw, string? defaultRegion, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        try
        {
            var region = string.IsNullOrWhiteSpace(defaultRegion) ? null : defaultRegion.Trim().ToUpperInvariant();
            var parsed = Util.Parse(raw.Trim(), region);
            if (!Util.IsValidNumber(parsed))
            {
                return false;
            }

            var type = Util.GetNumberType(parsed);
            if (type is not (PhoneNumberType.MOBILE or PhoneNumberType.FIXED_LINE_OR_MOBILE))
            {
                return false;
            }

            normalized = Util.Format(parsed, PhoneNumberFormat.E164);
            return true;
        }
        catch (NumberParseException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public string? NormalizeOrThrow(string? raw, string? defaultRegion)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return TryNormalize(raw, defaultRegion, out var normalized)
            ? normalized
            : throw new InvalidOperationException("手机号码格式无效。");
    }
}
