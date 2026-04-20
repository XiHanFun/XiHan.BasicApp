#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserNameValueObject.cs
// Guid:f8b2d6e9-3a7c-4f1b-e456-c0b3a2f7d9e1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// 用户名值对象
/// </summary>
public readonly record struct UserNameValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 50;

    public string Value { get; }

    private UserNameValueObject(string value)
    {
        Value = value;
    }

    /// <summary>
    /// 创建用户名值对象
    /// </summary>
    public static UserNameValueObject Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();
        if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
            throw new ArgumentException($"用户名长度必须在 {MinLength}-{MaxLength} 之间。");

        return new UserNameValueObject(trimmed);
    }

    public override string ToString() => Value;
}
