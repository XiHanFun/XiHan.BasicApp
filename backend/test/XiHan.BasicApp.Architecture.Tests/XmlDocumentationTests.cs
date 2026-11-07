// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;
using XiHan.BasicApp.Architecture.Tests.Model;

namespace XiHan.BasicApp.Architecture.Tests;

/// <summary>
/// XML 文档注释的写法守则
/// </summary>
/// <remarks>
/// 与框架 <c>SourceHygieneTests.XmlSummaries_AreOneConciseSentence</c> 同一套判定（框架 716db6db）：<c>&lt;summary&gt;</c> 三行写法、正文一行、
/// 显示宽度不超过 40（中日韩字符与全角标点算 1，其余算 0.5，向上取整）、只一句（没有「；」与「。」）
/// 注释（文档注释与行注释）句末一律不加「。」，一行里有多句时句间照常用「。」；注释里不写旧版本对照
/// 规则、错误码、路由与方案出处写进 <c>&lt;remarks&gt;</c>，参数、返回值与异常写进 <c>&lt;param&gt;</c>、<c>&lt;returns&gt;</c>、<c>&lt;exception&gt;</c>：
/// 生成的 OpenAPI 与 Scalar 拿 summary 当标题与字段说明，长段说明会把页面挤乱。src 与 test 下的源码都查，不分访问级别
/// </remarks>
public sealed partial class XmlDocumentationTests
{
    /// <summary>
    /// summary 的显示宽度上限
    /// </summary>
    public const int MaxSummaryWidth = 40;

    private static readonly string[] _scanned = ["src", "test"];

    [Fact]
    public void Summaries_AreOneConciseSentence_OnThreeLines()
    {
        var failures = new List<string>();
        foreach (var file in SourceFiles())
        {
            var lines = File.ReadAllLines(file);
            var where = Path.GetRelativePath(Packages.Root, file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed.StartsWith(Open, StringComparison.Ordinal) && trimmed.Length > Open.Length)
                {
                    failures.Add($"{where}:{i + 1} summary 须三行写法：'/// <summary>'、'/// 文案'、'/// </summary>'");
                    continue;
                }

                if (!string.Equals(trimmed, Open, StringComparison.Ordinal))
                {
                    continue;
                }

                var close = i + 1;
                while (close < lines.Length && !string.Equals(lines[close].Trim(), Close, StringComparison.Ordinal))
                {
                    close++;
                }

                if (close - i - 1 != 1)
                {
                    failures.Add($"{where}:{i + 1} summary 正文须一行，细节挪进 remarks");
                    continue;
                }

                var text = PlainText(lines[i + 1].Trim()[Comment.Length..]);
                var width = Width(text);
                if (width > MaxSummaryWidth || text.Contains('；', StringComparison.Ordinal) || text.Contains('。', StringComparison.Ordinal))
                {
                    failures.Add($"{where}:{i + 1} summary 须一句简明说明（宽度 {width}，上限 {MaxSummaryWidth}，不出现「；」与「。」），细节挪进 remarks：{text}");
                }
            }
        }

        Assert.True(failures.Count == 0, $"{failures.Count} 处 summary 不合规：\n" + string.Join('\n', failures));
    }

    [Fact]
    public void Comments_DoNotEndWithAFullStop()
    {
        var failures = new List<string>();
        foreach (var file in SourceFiles())
        {
            var lines = File.ReadAllLines(file);
            var where = Path.GetRelativePath(Packages.Root, file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith(LineComment, StringComparison.Ordinal) && TrailingStop().IsMatch(trimmed))
                {
                    failures.Add($"{where}:{i + 1} {trimmed}");
                }
            }
        }

        Assert.True(failures.Count == 0, $"{failures.Count} 处注释句末带「。」（结束标签之前的也算）：\n" + string.Join('\n', failures));
    }

    [Fact]
    public void Comments_DoNotCompareWithOldVersions()
    {
        var failures = new List<string>();
        foreach (var file in SourceFiles())
        {
            var lines = File.ReadAllLines(file);
            var where = Path.GetRelativePath(Packages.Root, file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith(LineComment, StringComparison.Ordinal) && OldVersion().IsMatch(trimmed))
                {
                    failures.Add($"{where}:{i + 1} {trimmed}");
                }
            }
        }

        Assert.True(failures.Count == 0, $"{failures.Count} 处注释写了旧版本对照，只写现在的事实：\n" + string.Join('\n', failures));
    }

    private static string LineComment { get; } = new('/', 2);

    // 拼出来而不是写成字面量，免得这几行自己被当成文档注释扫到
    private static string Comment { get; } = new('/', 3);

    private static string Open { get; } = Comment + " <summary>";

    private static string Close { get; } = Comment + " </summary>";

    private static IEnumerable<string> SourceFiles()
    {
        var separator = Path.DirectorySeparatorChar;
        return _scanned
            .Select(static folder => Path.Combine(Packages.Root, folder))
            .SelectMany(static folder => Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{separator}bin{separator}", StringComparison.Ordinal) && !path.Contains($"{separator}obj{separator}", StringComparison.Ordinal));
    }

    /// <summary>
    /// summary 的纯文本
    /// </summary>
    /// <remarks>
    /// 与框架同法：<c>&lt;see cref&gt;</c> 取最后一段名字，<c>&lt;see langword&gt;</c>、<c>&lt;paramref&gt;</c>、<c>&lt;typeparamref&gt;</c> 取名字，其余标签去掉，实体还原，空白折叠
    /// </remarks>
    private static string PlainText(string xml)
    {
        var text = SeeCref().Replace(xml, static m =>
        {
            var name = m.Groups["cref"].Value;
            var parameters = name.IndexOf('(', StringComparison.Ordinal);
            name = parameters >= 0 ? name[..parameters] : name;
            name = name[(name.LastIndexOf('.') + 1)..];
            var arity = name.IndexOfAny(['`', '{']);
            return arity >= 0 ? name[..arity] : name;
        });
        text = NamedReference().Replace(text, static m => m.Groups["name"].Value);
        text = AnyTag().Replace(text, string.Empty).Replace("&lt;", "<", StringComparison.Ordinal).Replace("&gt;", ">", StringComparison.Ordinal).Replace("&amp;", "&", StringComparison.Ordinal);
        return Whitespace().Replace(text, " ").Trim();
    }

    /// <summary>
    /// 显示宽度：中日韩字符与全角标点算 1，其余算 0.5，向上取整
    /// </summary>
    private static int Width(string text)
    {
        var halves = 0;
        foreach (var ch in text)
        {
            halves += ch is (>= '⺀' and <= '鿿') or (>= '가' and <= '힯') or (>= '豈' and <= '﫿') or (>= '＀' and <= '￯') ? 2 : 1;
        }

        return (halves + 1) / 2;
    }

    /// <summary>
    /// <c>&lt;see cref="…"/&gt;</c>
    /// </summary>
    [GeneratedRegex(@"<see\s+cref=""(?:[A-Z]:)?(?<cref>[^""]+)""\s*/>")]
    private static partial Regex SeeCref();

    /// <summary>
    /// <c>&lt;see langword&gt;</c>、<c>&lt;paramref&gt;</c> 与 <c>&lt;typeparamref&gt;</c>
    /// </summary>
    [GeneratedRegex(@"<(?:see\s+langword|paramref\s+name|typeparamref\s+name)=""(?<name>[^""]+)""\s*/>")]
    private static partial Regex NamedReference();

    /// <summary>
    /// 任意标签
    /// </summary>
    [GeneratedRegex("<[^>]+>")]
    private static partial Regex AnyTag();

    /// <summary>
    /// 连续空白
    /// </summary>
    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    /// <summary>
    /// 句末的句号（后面只跟结束标签与空白）
    /// </summary>
    [GeneratedRegex(@"。(?:\s*</[A-Za-z][\w:.-]*>)*\s*$")]
    private static partial Regex TrailingStop();

    /// <summary>
    /// 旧版本号：4 或 5 后跟「.x」
    /// </summary>
    /// <remarks>
    /// 前后不连字母数字，文件名 ip2region_v4.xdb 不算
    /// </remarks>
    [GeneratedRegex(@"(?<![\w.])[45]\.x(?!\w)")]
    private static partial Regex OldVersion();
}
