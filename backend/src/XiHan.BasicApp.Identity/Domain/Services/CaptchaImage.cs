// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 图形验证码的 SVG 画法
/// </summary>
/// <remarks>
/// 120×44。字符不写成文本：每个数字是 10×16 格子里的一笔骨架，随机缩放、旋转、倾斜并逐点抖动后画成路径，SVG 里读不到字符
/// 另画几条同样粗细与配色的干扰曲线和噪点，元素次序打乱；每次都重新取随机数，同一个码每次画出来都不一样
/// 只防直接读图的脚本，挡不住会识图的程序；要更强的人机验证得接第三方服务
/// </remarks>
public static class CaptchaImage
{
    private const int Width = 120;
    private const int Height = 44;

    private static readonly string[] _palette = ["#2563eb", "#dc2626", "#059669", "#7c3aed", "#d97706", "#0891b2"];

    // 数字的骨架：10×16 的格子，一笔或几笔折线，画的时候按中点连成平滑曲线
    private static readonly (double X, double Y)[][][] _glyphs =
    [
        [[(5, 0), (9, 2), (10, 8), (9, 14), (5, 16), (1, 14), (0, 8), (1, 2), (5, 0), (8, 1)]],
        [[(4.2, 2.2), (6, 0), (6, 16)]],
        [[(0.5, 4), (2, 1), (5, 0), (8.5, 1.5), (9.5, 4.5), (8, 8), (0, 16), (10, 16)]],
        [[(0.5, 2), (4, 0), (8, 1), (9, 4), (7.5, 7), (4, 8), (8, 9), (10, 12), (9, 15), (5, 16), (0.5, 14)]],
        [[(7, 16), (7, 0), (0, 11), (10, 11)]],
        [[(9, 0), (1.5, 0), (0.8, 7), (4, 6), (8, 7), (10, 10.5), (8.5, 14.5), (4.5, 16), (0.5, 14.5)]],
        [[(8.5, 1), (5, 0), (1.5, 2.5), (0, 8), (0.5, 13), (3, 16), (7, 15.5), (9.5, 12.5), (8.5, 9), (5, 7.5), (1.5, 9), (0.3, 11)]],
        [[(0, 0), (10, 0), (3, 16)], [(2.5, 8), (8, 8)]],
        [[(5, 8), (1.5, 6), (1, 2.5), (5, 0), (9, 2.5), (8.5, 6), (5, 8), (1, 10.5), (0.5, 13.5), (5, 16), (9.5, 13.5), (9, 10.5), (5, 8)]],
        [[(9.7, 5), (8.5, 7), (5, 8.5), (1.5, 7), (0.5, 3.5), (3, 0.5), (7, 0.3), (9.5, 3), (9.7, 8), (8.5, 13), (5, 16), (1.5, 15)]],
    ];

    /// <summary>
    /// 把验证码画成 data URI
    /// </summary>
    /// <returns><c>data:image/svg+xml;base64,</c> 链接，前端直接放进 <c>img</c></returns>
    public static string ToDataUri(string code) => "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(Render(code)));

    /// <summary>
    /// 画成 SVG 文本
    /// </summary>
    /// <exception cref="ArgumentException">码里有数字以外的字符</exception>
    public static string Render(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (!code.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("A captcha code must consist of ASCII digits.", nameof(code));
        }

        var random = new Random(RandomNumberGenerator.GetInt32(int.MaxValue));
        var elements = new List<string>();
        var span = (Width - 12.0) / code.Length;
        for (var index = 0; index < code.Length; index++)
        {
            elements.Add(Glyph(_glyphs[code[index] - '0'], 6 + (index * span), span, random));
        }

        for (var curve = 0; curve < 3; curve++)
        {
            elements.Add(NoiseCurve(random));
        }

        for (var dot = 0; dot < 18; dot++)
        {
            elements.Add(string.Create(CultureInfo.InvariantCulture, $"<circle cx=\"{N(random.NextDouble() * Width)}\" cy=\"{N(random.NextDouble() * Height)}\" r=\"{N(0.8 + random.NextDouble())}\" fill=\"{Color(random)}\" opacity=\"0.6\"/>"));
        }

        // 次序打乱：不能按元素先后认出哪几条是字符
        var builder = new StringBuilder(4096);
        builder.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Width}\" height=\"{Height}\" viewBox=\"0 0 {Width} {Height}\"><rect width=\"100%\" height=\"100%\" fill=\"#f8fafc\"/>");
        foreach (var element in elements.OrderBy(_ => random.Next()))
        {
            builder.Append(element);
        }

        builder.Append("</svg>");
        return builder.ToString();
    }

    /// <summary>
    /// 画一个字符
    /// </summary>
    /// <remarks>
    /// 骨架经随机缩放、倾斜、旋转与逐点抖动落到它的格子里
    /// </remarks>
    private static string Glyph((double X, double Y)[][] strokes, double left, double span, Random random)
    {
        var scale = 1.3 + (random.NextDouble() * 0.25);
        var skew = (random.NextDouble() - 0.5) * 0.5;
        var angle = (random.NextDouble() - 0.5) * 0.75;
        var (cos, sin) = (Math.Cos(angle), Math.Sin(angle));
        var centerX = left + (span / 2) + ((random.NextDouble() - 0.5) * 4);
        var centerY = (Height / 2.0) + ((random.NextDouble() - 0.5) * 4);
        var color = Color(random);
        var width = 2 + (random.NextDouble() * 0.8);
        var path = new StringBuilder(512);
        foreach (var stroke in strokes)
        {
            var points = stroke.Select(p =>
            {
                var x = (p.X - 5 + ((random.NextDouble() - 0.5) * 1.2)) * scale;
                var y = (p.Y - 8 + ((random.NextDouble() - 0.5) * 1.2)) * scale;
                x += skew * y;
                return (X: centerX + (x * cos) - (y * sin), Y: centerY + (x * sin) + (y * cos));
            }).ToArray();
            AppendSmooth(path, points);
        }

        return string.Create(CultureInfo.InvariantCulture, $"<path d=\"{path}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"{N(width)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
    }

    /// <summary>
    /// 横穿画面的干扰曲线
    /// </summary>
    /// <remarks>
    /// 粗细与配色和字符的笔画相同，不能按样式滤掉
    /// </remarks>
    private static string NoiseCurve(Random random)
    {
        (double X, double Y) Point(double x) => (x, 6 + (random.NextDouble() * (Height - 12)));
        var points = new[] { Point(random.NextDouble() * 20), Point(30 + (random.NextDouble() * 20)), Point(70 + (random.NextDouble() * 20)), Point(100 + (random.NextDouble() * 20)) };
        var path = new StringBuilder(128);
        AppendSmooth(path, points);
        return string.Create(CultureInfo.InvariantCulture, $"<path d=\"{path}\" fill=\"none\" stroke=\"{Color(random)}\" stroke-width=\"{N(1.2 + random.NextDouble())}\" stroke-linecap=\"round\" opacity=\"0.85\"/>");
    }

    /// <summary>
    /// 折线按中点连成二次曲线
    /// </summary>
    private static void AppendSmooth(StringBuilder path, (double X, double Y)[] points)
    {
        path.Append(CultureInfo.InvariantCulture, $"M{N(points[0].X)} {N(points[0].Y)}");
        for (var i = 1; i < points.Length - 1; i++)
        {
            var (midX, midY) = ((points[i].X + points[i + 1].X) / 2, (points[i].Y + points[i + 1].Y) / 2);
            path.Append(CultureInfo.InvariantCulture, $"Q{N(points[i].X)} {N(points[i].Y)} {N(midX)} {N(midY)}");
        }

        path.Append(CultureInfo.InvariantCulture, $"L{N(points[^1].X)} {N(points[^1].Y)}");
    }

    private static string Color(Random random) => _palette[random.Next(_palette.Length)];

    private static string N(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}
