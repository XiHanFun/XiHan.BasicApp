// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// 业务引用
/// </summary>
public sealed record BusinessReference(string? BusinessType, long? BusinessId)
{
    /// <summary>
    /// 空引用
    /// </summary>
    public static BusinessReference Empty { get; } = new(null, null);
}
