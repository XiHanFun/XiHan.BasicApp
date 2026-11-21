// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;

namespace XiHan.BasicApp.Contracts.Pages;

/// <summary>
/// 菜单徽标的缺省值
/// </summary>
/// <remarks>
/// 页面登记与内置节点用它给菜单项右侧的徽标：圆点优先于文字，语气与组件库的 tone 同名
/// </remarks>
public sealed partial record MenuBadge
{
    /// <summary>
    /// 语气的取值
    /// </summary>
    public const string TonePattern = "^(neutral|brand|info|success|warning|danger)$";

    /// <summary>
    /// 文字的最大长度
    /// </summary>
    public const int MaxTextLength = 50;

    /// <summary>
    /// 构造菜单徽标
    /// </summary>
    /// <exception cref="ArgumentException">文字超长、语气不在取值里，或文字与圆点都没有时抛出</exception>
    public MenuBadge(string? text = null, string? tone = null, bool dot = false)
    {
        if (text is { Length: > MaxTextLength })
        {
            throw new ArgumentException($"Menu badge text must be at most {MaxTextLength} characters.", nameof(text));
        }

        if (tone is not null && !ToneRegex().IsMatch(tone))
        {
            throw new ArgumentException($"Menu badge tone '{tone}' is not one of the component library tones.", nameof(tone));
        }

        if (string.IsNullOrWhiteSpace(text) && !dot)
        {
            throw new ArgumentException("Menu badge needs text or a dot.", nameof(text));
        }

        Text = text;
        Tone = tone;
        Dot = dot;
    }

    /// <summary>
    /// 徽标文字，如 <c>new</c>
    /// </summary>
    public string? Text { get; }

    /// <summary>
    /// 语气：为空时文字取中性、圆点取提醒红
    /// </summary>
    public string? Tone { get; }

    /// <summary>
    /// 是否显示圆点（优先于文字）
    /// </summary>
    public bool Dot { get; }

    [GeneratedRegex(TonePattern)]
    private static partial Regex ToneRegex();
}
