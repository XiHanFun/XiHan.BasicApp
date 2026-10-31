// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>字典项的标签显示契约；只接受颜色与离线图标标识，不接收HTML、CSS表达式或业务状态规则。</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DictTagStyle
{
    /// <summary>XiHan.UI语义色；留空采用组件默认。</summary>
    [RegularExpression("^(brand|neutral|success|warning|danger|info)\\z")]
    public string? Tone { get; init; }
    /// <summary>自定义背景色，严格六位十六进制；null表示跟随语义色。</summary>
    [RegularExpression("^#[0-9A-Fa-f]{6}\\z")]
    public string? BackgroundColor { get; init; }
    /// <summary>标签文字前景色，严格六位十六进制；null表示跟随语义色。</summary>
    [RegularExpression("^#[0-9A-Fa-f]{6}\\z")]
    public string? TextColor { get; init; }
    /// <summary>是否显示图标；null沿用默认显示，false隐藏但保留自定义图标。</summary>
    public bool? ShowIcon { get; init; }
    /// <summary>菜单图标选择器的离线图标标识；null跟随当前配色的默认图标。</summary>
    [MaxLength(DictTagStyles.MaxIconLength)]
    [RegularExpression(DictTagStyles.IconPattern)]
    public string? Icon { get; init; }
}

/// <summary>在既有Metadata中维护独立tagStyle节点；无共享可变状态，事务与并发由原字典仓储负责。</summary>
public static partial class DictTagStyles
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string Key = "tagStyle";
    /// <summary>图标ID的最大长度，与前端显示契约一致。</summary>
    public const int MaxIconLength = 160;
    /// <summary>仅允许菜单选择器已安装的图标集及名称，不接受URL、SVG或远程Iconify提供者。</summary>
    public const string IconPattern = "^(carbon|ep|heroicons|lucide|mdi|simple-icons|tabler):[a-z0-9]+(?:-[a-z0-9]+)*\\z";

    /// <summary>读取已保存的显示配置，不向选项接口暴露其他扩展数据。</summary>
    /// <param name="metadata">既有JSON。</param><returns>样式或null。</returns>
    /// <exception cref="ValidationException">样式不合法。</exception>
    public static DictTagStyle? Read(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata)) return null;
        using var document = JsonDocument.Parse(metadata);
        if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty(Key, out var node) || node.ValueKind == JsonValueKind.Null) return null;
        return Validate(node.Deserialize<DictTagStyle>(Json) ?? throw new ValidationException("标签样式不能为空对象以外的值。"));
    }

    /// <summary>仅替换标签节点，保留所有无关元数据；省略样式时保持原JSON。</summary>
    /// <param name="metadata">完整元数据。</param><param name="style">null表示不修改；空对象表示清除自定义样式。</param>
    /// <returns>合并后的JSON。</returns><exception cref="ValidationException">颜色非法，或元数据不是可扩展对象。</exception>
    public static string? Merge(string? metadata, DictTagStyle? style)
    {
        if (style is null) { _ = Read(metadata); return metadata; }
        var normalized = Validate(style);
        // 原样回交时保留原JSON字节，避免只读元数据因格式化或补null属性产生隐性变化。
        if (Read(metadata) == normalized) return metadata;
        var root = string.IsNullOrWhiteSpace(metadata) ? new JsonObject() : JsonNode.Parse(metadata) as JsonObject
            ?? throw new ValidationException("设置标签样式时，扩展元数据必须是JSON对象；已有内容不会被清空。");
        if (normalized.Tone is null && normalized.BackgroundColor is null && normalized.TextColor is null && normalized.ShowIcon is null && normalized.Icon is null) root.Remove(Key);
        else root[Key] = JsonSerializer.SerializeToNode(normalized, Json);
        return root.Count == 0 ? null : root.ToJsonString(Json);
    }

    /// <summary>校验颜色和图标；必须在持久化前执行，不能只信任前端选择器。</summary>
    /// <param name="style">输入样式。</param><returns>安全的显示配置。</returns><exception cref="ValidationException">输入非法。</exception>
    public static DictTagStyle Validate(DictTagStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        if (style.Tone is not (null or "brand" or "neutral" or "success" or "warning" or "danger" or "info")) throw new ValidationException("标签预设配色无效。");
        if (style.Icon is not null && (style.Icon.Length > MaxIconLength || !IconId().IsMatch(style.Icon))) throw new ValidationException("图标必须是系统图标选择器中的图标标识，例如lucide:check。");
        return style with { BackgroundColor = Color(style.BackgroundColor), TextColor = Color(style.TextColor) };
    }

    // 拒绝CSS函数、url、变量和透明表达式；null才表示跟随主题。
    private static string? Color(string? value) => value is null ? null : HexColor().IsMatch(value) ? value.ToUpperInvariant() : throw new ValidationException("颜色必须是#加六位十六进制，例如#1677FF。");
    // 只接受完整六位字面量，绝对结束锚点拒绝尾随换行。
    [GeneratedRegex("^#[0-9A-Fa-f]{6}\\z", RegexOptions.CultureInvariant)]
    private static partial Regex HexColor();
    // 离线图标集白名单与名称语法；不接收远程提供者。
    [GeneratedRegex(IconPattern, RegexOptions.CultureInvariant)]
    private static partial Regex IconId();
}
