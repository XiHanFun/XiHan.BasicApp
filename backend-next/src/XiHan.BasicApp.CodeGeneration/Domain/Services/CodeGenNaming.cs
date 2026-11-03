// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;

namespace XiHan.BasicApp.CodeGeneration.Domain.Services;

/// <summary>
/// 代码生成的命名约定
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>NamingConventions</c>：类名、属性名、kebab、snake 与 camel 之间换算
/// </remarks>
public static partial class CodeGenNaming
{
    /// <summary>
    /// 首字母小写
    /// </summary>
    /// <param name="value">原名</param>
    public static string Camelize(string value)
        => string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];

    /// <summary>
    /// 换成 kebab 写法
    /// </summary>
    /// <param name="value">原名（PascalCase、camelCase 或 snake_case）</param>
    public static string Kebabize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var result = AcronymBoundary().Replace(value, "$1-$2");
        result = WordBoundary().Replace(result, "$1-$2");
        return result.Replace('_', '-').ToLowerInvariant();
    }

    /// <summary>
    /// 换成 PascalCase
    /// </summary>
    /// <param name="value">原名（下划线、空格或连字符分段）</param>
    public static string Pascalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var segments = value.Split(['_', ' ', '-'], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(segments.Select(static segment => char.ToUpperInvariant(segment[0]) + (segment.Length > 1 ? segment[1..] : string.Empty)));
    }

    /// <summary>
    /// 换成 snake_case
    /// </summary>
    /// <param name="value">原名</param>
    public static string Snakeize(string value)
        => string.IsNullOrEmpty(value) ? value : Kebabize(value).Replace('-', '_');

    /// <summary>
    /// 去掉表名前缀
    /// </summary>
    /// <remarks>
    /// 取最长的匹配前缀，不分大小写；去掉之后为空就不去（照 4.x）
    /// </remarks>
    /// <param name="tableName">表名</param>
    /// <param name="prefixes">前缀</param>
    public static string StripPrefix(string tableName, IReadOnlyList<string> prefixes)
    {
        ArgumentNullException.ThrowIfNull(prefixes);
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return tableName;
        }

        var best = prefixes
            .Where(prefix => !string.IsNullOrWhiteSpace(prefix) && tableName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && tableName.Length > prefix.Length)
            .OrderByDescending(static prefix => prefix.Length)
            .FirstOrDefault();
        return best is null ? tableName : tableName[best.Length..];
    }

    [GeneratedRegex("([A-Z]+)([A-Z][a-z])")]
    private static partial Regex AcronymBoundary();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex WordBoundary();
}
