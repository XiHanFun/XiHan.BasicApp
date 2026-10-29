// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Notification.Queries;

/// <summary>
/// 筛选值里的枚举（逗号分隔、按名称、不区分大小写）。
/// </summary>
internal static class Enums
{
    /// <summary>
    /// 解析；有一项不认识或为空时为 <see langword="null"/>。
    /// </summary>
    /// <typeparam name="TEnum">枚举类型。</typeparam>
    /// <param name="value">筛选值。</param>
    /// <returns>枚举值。</returns>
    public static List<TEnum>? Of<TEnum>(string value)
        where TEnum : struct, Enum
    {
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var parsed = new List<TEnum>(parts.Length);
        foreach (var part in parts)
        {
            if (!Enum.TryParse<TEnum>(part, ignoreCase: true, out var item) || !Enum.IsDefined(item) || int.TryParse(part, out _))
            {
                return null;
            }

            parsed.Add(item);
        }

        return parsed.Count == 0 ? null : parsed;
    }
}
