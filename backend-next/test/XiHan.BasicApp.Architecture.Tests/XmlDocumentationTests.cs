// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;
using XiHan.BasicApp.Architecture.Tests.Model;

namespace XiHan.BasicApp.Architecture.Tests;

/// <summary>
/// XML 文档注释的写法守则。
/// </summary>
/// <remarks>
/// <c>&lt;summary&gt;</c> 只写一句简明说明：三行写法，去掉标签后不超过 40 字，句中不出现「。」「；」（结尾可以有一个句号），不写与 4.x 的对照。
/// 规则、错误码、路由、4.x 差异与方案出处写进 <c>&lt;remarks&gt;</c>，参数、返回值与异常写进 <c>&lt;param&gt;</c>、<c>&lt;returns&gt;</c>、<c>&lt;exception&gt;</c>：
/// 生成的 OpenAPI 与 Scalar 拿 summary 当标题，长段说明会把页面挤乱。src 与 test 下的源码都查。
/// </remarks>
public sealed partial class XmlDocumentationTests
{
    /// <summary>
    /// summary 去掉标签后的字数上限。
    /// </summary>
    public const int MaxSummaryLength = 40;

    private static readonly string[] _scanned = ["src", "test"];

    [Fact]
    public void Summaries_AreOneShortSentence_OnThreeLines()
    {
        var problems = new List<string>();
        foreach (var file in SourceFiles())
        {
            var text = File.ReadAllText(file).ReplaceLineEndings("\n");
            var where = Path.GetRelativePath(Packages.Root, file);
            foreach (Match single in SingleLine().Matches(text))
            {
                problems.Add($"{where}:{LineOf(text, single.Index)} 单行 summary");
            }

            foreach (Match block in Block().Matches(text))
            {
                var plain = PlainOf(block.Groups[1].Value);
                var body = plain.TrimEnd('。', '.');
                var length = plain.EnumerateRunes().Count();
                if (length > MaxSummaryLength)
                {
                    problems.Add($"{where}:{LineOf(text, block.Index)} 超过 {MaxSummaryLength} 字（{length}）：{plain}");
                }
                else if (body.Contains('。', StringComparison.Ordinal) || body.Contains('；', StringComparison.Ordinal))
                {
                    problems.Add($"{where}:{LineOf(text, block.Index)} 不止一句：{plain}");
                }
                else if (plain.Contains("4.x", StringComparison.Ordinal))
                {
                    problems.Add($"{where}:{LineOf(text, block.Index)} 与 4.x 的对照写进 remarks：{plain}");
                }
            }
        }

        Assert.True(problems.Count == 0, $"{problems.Count} 处 summary 不合规（细节挪进 remarks / param / returns / exception）：\n" + string.Join('\n', problems));
    }

    private static IEnumerable<string> SourceFiles()
    {
        var separator = Path.DirectorySeparatorChar;
        return _scanned
            .Select(static folder => Path.Combine(Packages.Root, folder))
            .SelectMany(static folder => Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{separator}bin{separator}", StringComparison.Ordinal) && !path.Contains($"{separator}obj{separator}", StringComparison.Ordinal));
    }

    /// <summary>
    /// 取 summary 的纯文本。
    /// </summary>
    /// <remarks>
    /// <c>&lt;c&gt;</c> 计其中的文字，<c>&lt;see cref&gt;</c> 计最后一段名字，<c>&lt;paramref&gt;</c> 计参数名。
    /// </remarks>
    private static string PlainOf(string inner)
    {
        var joined = string.Concat(inner.Split('\n').Select(static line => CommentPrefix().Replace(line, string.Empty, 1)));
        var named = Reference().Replace(joined, static m => m.Groups[1].Value.Split('.')[^1]);
        return Tag().Replace(named, string.Empty).Trim();
    }

    private static int LineOf(string text, int index) => text.AsSpan(0, index).Count('\n') + 1;

    // 斜杠写成 /{3}，免得这几个正则自己被当成文档注释扫到
    [GeneratedRegex(@"/{3} <summary>\n([\s\S]*?)/{3} </summary>")]
    private static partial Regex Block();

    [GeneratedRegex(@"/{3} <summary>[^\n]*</summary>")]
    private static partial Regex SingleLine();

    [GeneratedRegex(@"^\s*/{3} ?")]
    private static partial Regex CommentPrefix();

    [GeneratedRegex(@"<(?:see|paramref|typeparamref)\s+(?:cref|langword|href|name)=""([^""]*)""\s*/>")]
    private static partial Regex Reference();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tag();
}
