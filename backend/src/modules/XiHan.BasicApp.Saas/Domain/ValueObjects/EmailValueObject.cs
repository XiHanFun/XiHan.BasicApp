#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailValueObject.cs
// Guid:a9c3e7f0-4b8d-4a2c-f567-d1c4b3a8e0f2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.RegularExpressions;

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// 邮箱值对象
/// </summary>
public readonly partial record struct EmailValueObject
{
    public string Value { get; }

    private EmailValueObject(string value)
    {
        Value = value;
    }

    /// <summary>
    /// 创建邮箱值对象
    /// </summary>
    public static EmailValueObject Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim().ToLowerInvariant();
        if (!EmailRegex().IsMatch(trimmed))
            throw new ArgumentException("邮箱格式不正确。");

        return new EmailValueObject(trimmed);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
