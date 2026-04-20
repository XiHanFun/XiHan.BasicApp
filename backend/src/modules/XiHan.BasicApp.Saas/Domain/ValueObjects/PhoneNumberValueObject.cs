#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PhoneNumberValueObject.cs
// Guid:b0d4f8a1-5c9e-4b3d-a678-e2d5c4b9f1a3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.RegularExpressions;

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// 手机号值对象
/// </summary>
public readonly partial record struct PhoneNumberValueObject
{
    public string Value { get; }

    private PhoneNumberValueObject(string value)
    {
        Value = value;
    }

    /// <summary>
    /// 创建手机号值对象
    /// </summary>
    public static PhoneNumberValueObject Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();
        if (!PhoneRegex().IsMatch(trimmed))
            throw new ArgumentException("手机号格式不正确。");

        return new PhoneNumberValueObject(trimmed);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^1[3-9]\d{9}$")]
    private static partial Regex PhoneRegex();
}
