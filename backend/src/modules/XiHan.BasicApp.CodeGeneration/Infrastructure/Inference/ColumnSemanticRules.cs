// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Inference;

/// <summary>
/// 列名语义规则：按列名把控件类型推断得更贴合，覆盖类型映射给出的默认控件
/// </summary>
/// <remarks>
/// 在 DB 类型映射之后应用，只对 string 类文本列生效（数值/布尔/时间由类型映射决定，语义无需干预）。
/// 规则顺序敏感、首个命中即止：如 <c>icon_url</c> 应命中 icon（上传）而非 url。
/// </remarks>
public static class ColumnSemanticRules
{
    /// <summary>
    /// 规则表（关键字片段 → 控件），按优先级从高到低排列
    /// </summary>
    private static readonly (string[] Keywords, HtmlType Html)[] Rules =
    [
        (["icon", "img", "image", "avatar", "logo", "photo", "cover", "banner"], HtmlType.ImageUpload),
        (["pic", "picture"], HtmlType.ImageUpload),
        (["file", "attachment", "annex"], HtmlType.FileUpload),
        (["color", "colour"], HtmlType.Input),
        (["content", "description", "remark", "detail", "intro", "summary", "note"], HtmlType.Textarea),
    ];

    /// <summary>
    /// 按列名推断控件；未命中返回 null（沿用类型映射的默认）
    /// </summary>
    /// <param name="columnName">数据库列名</param>
    /// <param name="length">列长度（用于长文本判定）</param>
    public static HtmlType? Infer(string columnName, int? length)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return null;
        }

        var lower = columnName.ToLowerInvariant();
        foreach (var (keywords, html) in Rules)
        {
            if (keywords.Any(keyword => lower.Contains(keyword, StringComparison.Ordinal)))
            {
                return html;
            }
        }

        // 超长字符串列即使名字无语义，也更适合多行输入
        if (length is > 500)
        {
            return HtmlType.Textarea;
        }

        return null;
    }
}
