#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantCodeValueObject.cs
// Guid:c1e5a9b2-6d0f-4c4e-b789-f3e6d5c0a2b4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.RegularExpressions;

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// 租户编码值对象
/// </summary>
public readonly partial record struct TenantCodeValueObject
{
    private const int MinLength = 2;
    private const int MaxLength = 50;

    public string Value { get; }

    private TenantCodeValueObject(string value)
    {
        Value = value;
    }

    /// <summary>
    /// 创建租户编码值对象
    /// </summary>
    public static TenantCodeValueObject Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim().ToLowerInvariant();
        if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
            throw new ArgumentException($"租户编码长度必须在 {MinLength}-{MaxLength} 之间。");

        if (!TenantCodeRegex().IsMatch(trimmed))
            throw new ArgumentException("租户编码只能包含小写字母、数字和连字符。");

        return new TenantCodeValueObject(trimmed);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-z0-9][a-z0-9\-]*[a-z0-9]$")]
    private static partial Regex TenantCodeRegex();
}
