// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
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
    /// PascalCase → snake_case（SysProduct → sys_product，与前端文案键段一致）
    /// </summary>
    public static string Snakeize(string value)
    {
        return string.IsNullOrEmpty(value) ? value : Kebabize(value).Replace('-', '_');
    }

    /// <summary>
    /// 任意文本 → i18n 键段
    /// </summary>
    /// <remarks>
    /// 前端门禁的孤儿扫描正则只认 <c>[a-z]\w*</c>：连字符或非 ASCII 会让整条 t() 调用不被匹配，
    /// 门禁显示通过而页面运行期渲染裸键。此处 fail-closed 到确定性哈希，
    /// 保证任何输入都产出合规且互不相同的键段。
    /// </remarks>
    /// <param name="value">原始文本（标识符或名称）</param>
    /// <returns>形如 <c>[a-z][a-z0-9_]*</c> 的键段</returns>
    public static string I18nSegment(string? value)
    {
        var snake = Snakeize(value ?? string.Empty);
        var builder = new StringBuilder(snake.Length);
        foreach (var ch in snake)
        {
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(ch);
            }
            else if (builder.Length > 0 && builder[^1] != '_')
            {
                builder.Append('_');
            }
        }

        var result = builder.ToString().Trim('_');
        if (result.Length > 0 && result[0] is >= 'a' and <= 'z')
        {
            return result;
        }

        // 全非 ASCII（中文列名/模块名）：退到确定性哈希，可读性差但唯一且合规
        var hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)))[..8];
        return result.Length > 0 ? $"k_{result}" : $"k_{hash.ToLowerInvariant()}";
    }

    /// <summary>
    /// 标识符 → 空格分隔英文标签（ProductName → Product Name）
    /// </summary>
    /// <param name="value">标识符</param>
    /// <returns>英文标签；输入为空时返回空串</returns>
    public static string HumanizeIdentifier(string? value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : string.Join(' ', Kebabize(value)
                .Split('-', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
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
