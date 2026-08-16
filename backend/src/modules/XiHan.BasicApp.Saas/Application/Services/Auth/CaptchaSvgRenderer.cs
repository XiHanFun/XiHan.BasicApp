// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 验证码 SVG 渲染器：把数字码绘制成带旋转/抖动/干扰线的图片。
/// </summary>
/// <remarks>
/// 扰动参数全部由码字符号确定性派生（无随机源）：同一码输出稳定，便于测试断言；
/// 不同码形态不同，干扰线起到简单的图像识别阻碍作用。
/// </remarks>
public static class CaptchaSvgRenderer
{
    private const int Width = 120;

    private const int Height = 44;

    private static readonly string[] Palette =
    [
        "#2563eb",
        "#dc2626",
        "#059669",
        "#7c3aed",
        "#d97706",
        "#0891b2"
    ];

    /// <summary>
    /// 渲染验证码 SVG
    /// </summary>
    /// <param name="code">待绘制的数字码</param>
    /// <returns>SVG 标记文本</returns>
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
            var jitter = (seed * 5 + index * 3) % 7 - 3;
            var rotation = (seed * 7 + index * 11) % 25 - 12;
            var color = Palette[(seed + index) % Palette.Length];
            var x = startX + index * span + 8 + jitter;
            var y = 30 + (seed % 5) - 2;
            builder.Append("<text x=\"").Append(x).Append("\" y=\"").Append(y).Append("\" font-size=\"24\" font-family=\"monospace\" font-weight=\"700\" fill=\"").Append(color).Append("\" transform=\"rotate(").Append(rotation).Append(' ').Append(x).Append(' ').Append(y).Append(")\">").Append(code[index]).Append("</text>");
        }

        // 两条干扰线 + 噪点（确定性派生，不引入随机源）
        for (var line = 0; line < 2; line++)
        {
            var seed = code[(line * 2) % code.Length];
            var x1 = seed % 20;
            var y1 = 8 + (seed + line * 17) % (Height - 16);
            var x2 = Width - (seed + line * 13) % 30;
            var y2 = 8 + (seed * 3 + line * 7) % (Height - 16);
            builder.Append("<line x1=\"").Append(x1).Append("\" y1=\"").Append(y1).Append("\" x2=\"").Append(x2).Append("\" y2=\"").Append(y2).Append("\" stroke=\"#94a3b8\" stroke-width=\"1\" opacity=\"0.55\"/>");
        }

        for (var dot = 0; dot < 6; dot++)
        {
            var seed = code[dot % code.Length];
            var cx = (seed * 7 + dot * 19) % Width;
            var cy = (seed * 11 + dot * 13) % Height;
            builder.Append("<circle cx=\"").Append(cx).Append("\" cy=\"").Append(cy).Append("\" r=\"1.2\" fill=\"#cbd5e1\"/>");
        }

        builder.Append("</svg>");
        return builder.ToString();
    }
}
