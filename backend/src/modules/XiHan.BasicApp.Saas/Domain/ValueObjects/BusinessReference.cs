#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:BusinessReference
// Guid:a4814d5a-7830-4a41-aae2-890e4d6bb5a9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
