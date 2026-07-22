// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 生成产物的命名转换（引擎与渲染器共用，避免多处实现漂移）
/// </summary>
public static partial class NamingConventions
{
    /// <summary>
    /// PascalCase → camelCase（首字母小写，其余不变）
    /// </summary>
    public static string Camelize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// PascalCase → kebab-case（与前端 toKebabCase 一致：SysProduct → sys-product）
    /// </summary>
    public static string Kebabize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var result = AcronymBoundaryRegex().Replace(value, "$1-$2");
        result = WordBoundaryRegex().Replace(result, "$1-$2");
        return result.Replace('_', '-').ToLowerInvariant();
    }

    /// <summary>
    /// 把下划线/空格/连字符分隔的标识转为 PascalCase（如 sys_user → SysUser）
    /// </summary>
    public static string Pascalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var segments = value.Split(['_', ' ', '-'], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(segments.Select(segment =>
            char.ToUpperInvariant(segment[0]) + (segment.Length > 1 ? segment[1..] : string.Empty)));
    }

    /// <summary>
    /// 连续大写后接单词的边界（HTTPServer → HTTP-Server）
    /// </summary>
    [GeneratedRegex("([A-Z]+)([A-Z][a-z])")]
    private static partial Regex AcronymBoundaryRegex();

    /// <summary>
    /// 小写数字后接大写的边界（sysUser → sys-User）
    /// </summary>
    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex WordBoundaryRegex();
}
