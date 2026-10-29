// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 图形验证码的 SVG 画法。
/// </summary>
/// <remarks>
/// 120×44，与 4.x 同款。每个字符的抖动、旋转、颜色与高度由字符码与位置派生，另有两条干扰线与六个噪点；不引入随机源，同一个码画出同一张图。
/// </remarks>
public static class CaptchaImage
{
    private const int Width = 120;
    private const int Height = 44;

    private static readonly string[] _palette = ["#2563eb", "#dc2626", "#059669", "#7c3aed", "#d97706", "#0891b2"];

    /// <summary>
    /// 把验证码画成 data URI。
    /// </summary>
    /// <returns><c>data:image/svg+xml;base64,</c> 链接，前端直接放进 <c>img</c>。</returns>
    public static string ToDataUri(string code) => "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(Render(code)));

    /// <summary>
    /// 画成 SVG 文本。
    /// </summary>
    public static string Render(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var builder = new StringBuilder(1024);
        builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"120\" height=\"44\" viewBox=\"0 0 120 44\"><rect width=\"100%\" height=\"100%\" fill=\"#f8fafc\"/>");
        const int span = 26;
        const int startX = 12;
        for (var index = 0; index < code.Length; index++)
        {
            var seed = code[index];
            var jitter = ((seed * 5) + (index * 3)) % 7 - 3;
            var rotation = ((seed * 7) + (index * 11)) % 25 - 12;
            var color = _palette[(seed + index) % _palette.Length];
            var x = startX + (index * span) + 8 + jitter;
            var y = 30 + (seed % 5) - 2;
            builder.Append(CultureInfo.InvariantCulture, $"<text x=\"{x}\" y=\"{y}\" font-size=\"24\" font-family=\"monospace\" font-weight=\"700\" fill=\"{color}\" transform=\"rotate({rotation} {x} {y})\">{code[index]}</text>");
        }

        for (var line = 0; line < 2; line++)
        {
            var seed = code[(line * 2) % code.Length];
            var x1 = seed % 20;
            var y1 = 8 + ((seed + (line * 17)) % (Height - 16));
            var x2 = Width - ((seed + (line * 13)) % 30);
            var y2 = 8 + (((seed * 3) + (line * 7)) % (Height - 16));
            builder.Append(CultureInfo.InvariantCulture, $"<line x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\" stroke=\"#94a3b8\" stroke-width=\"1\" opacity=\"0.55\"/>");
        }

        for (var dot = 0; dot < 6; dot++)
        {
            var seed = code[dot % code.Length];
            var cx = ((seed * 7) + (dot * 19)) % Width;
            var cy = ((seed * 11) + (dot * 13)) % Height;
            builder.Append(CultureInfo.InvariantCulture, $"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"1.2\" fill=\"#cbd5e1\"/>");
        }

        builder.Append("</svg>");
        return builder.ToString();
    }
}
