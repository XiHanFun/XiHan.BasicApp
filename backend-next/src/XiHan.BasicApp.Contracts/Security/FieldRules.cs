// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
using XiHan.BasicApp.Contracts.Paging;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 当前主体在一个字段上的有效规则（多条命中已合并）。
/// </summary>
/// <param name="Field">字段名。</param>
/// <param name="Strategy">读取方式。</param>
/// <param name="KeepHead">部分脱敏保留前几位。</param>
/// <param name="KeepTail">部分脱敏保留后几位。</param>
/// <param name="Replacement">固定文本。</param>
/// <param name="IsEditable">是否可改；读受保护且可改即只写（看不到原值但能填新值）。</param>
public sealed record FieldRule(string Field, FieldMaskStrategy Strategy, int? KeepHead, int? KeepTail, string? Replacement, bool IsEditable)
{
    /// <summary>
    /// 读受保护：不再明文返回，也不能拿来筛选、排序或关键字搜索。
    /// </summary>
    /// <remarks>
    /// 不能筛选、排序或搜索，否则可按结果反推原值。
    /// </remarks>
    public bool IsReadProtected => Strategy != FieldMaskStrategy.None;

    /// <summary>
    /// 一个文本值在本规则下对外呈现的样子。
    /// </summary>
    /// <returns>呈现的值；空值原样返回（隐藏一律返回空）。</returns>
    public string? Mask(string? raw)
    {
        if (Strategy == FieldMaskStrategy.None)
        {
            return raw;
        }

        if (Strategy == FieldMaskStrategy.Hidden || string.IsNullOrEmpty(raw))
        {
            return Strategy == FieldMaskStrategy.Hidden ? null : raw;
        }

        return Strategy switch
        {
            FieldMaskStrategy.FullMask => new string('*', raw.Length),
            FieldMaskStrategy.PartialMask => PartialMask(raw, KeepHead ?? 0, KeepTail ?? 0),
            FieldMaskStrategy.Hash => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16].ToLowerInvariant(),
            FieldMaskStrategy.Redact => Replacement ?? string.Empty,
            _ => throw new InvalidOperationException($"Unknown mask strategy {Strategy}."),
        };
    }

    private static string PartialMask(string raw, int keepHead, int keepTail)
        => keepHead + keepTail >= raw.Length
            ? new string('*', raw.Length)
            : string.Concat(raw.AsSpan(0, keepHead), new string('*', raw.Length - keepHead - keepTail), raw.AsSpan(raw.Length - keepTail));
}

/// <summary>
/// 写入时对一个受保护字段的处理。
/// </summary>
public enum FieldWrite
{
    /// <summary>
    /// 照收入参。
    /// </summary>
    Accept,

    /// <summary>
    /// 入参是原值、脱敏值或空值：视为没改，保留库里的原值（免得脱敏值覆盖真实数据）。
    /// </summary>
    Keep,

    /// <summary>
    /// 改动了只读字段：拒绝。
    /// </summary>
    Denied,
}

/// <summary>
/// 当前主体在一个实体上的有效字段规则。
/// </summary>
/// <remarks>
/// 方案 5.9 字段安全。由 <see cref="IFieldSecurity"/> 解析，各包在投影、查询与写入处自己套用：
/// 投影成响应前 <see cref="Mask(string, string?)"/>；查询前 <see cref="FirstProtected"/> 拒绝按受保护字段筛选与排序、<see cref="CanSearch"/> 把它们移出关键字搜索；
/// 新建前 <see cref="OnCreate"/>，修改前 <see cref="OnUpdate"/>。
/// </remarks>
public sealed class FieldRules
{
    private readonly Dictionary<string, FieldRule> _rules;

    /// <summary>
    /// 由已合并的规则构造（每个字段一条）。
    /// </summary>
    public FieldRules(IEnumerable<FieldRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules.ToDictionary(static r => r.Field, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 没有规则（没装 Access、未登录或没有命中）。
    /// </summary>
    public static FieldRules None { get; } = new([]);

    /// <summary>
    /// 是否一条规则都没有。
    /// </summary>
    public bool IsEmpty => _rules.Count == 0;

    /// <summary>
    /// 全部有效规则。
    /// </summary>
    public IReadOnlyCollection<FieldRule> All => _rules.Values;

    /// <summary>
    /// 同一字段多条规则合并。
    /// </summary>
    /// <remarks>
    /// 沿用 4.x：读取方式取最严，部分脱敏的保留位数取各条最少，固定文本取第一条，任一只读即只读。
    /// </remarks>
    /// <param name="rules">命中的规则，按创建先后排好。</param>
    public static FieldRules Merge(IEnumerable<FieldRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return new FieldRules(rules.GroupBy(static r => r.Field, StringComparer.OrdinalIgnoreCase).Select(static group =>
        {
            var strategy = group.MaxBy(static r => Strictness(r.Strategy))!.Strategy;
            var partials = group.Where(static r => r.Strategy == FieldMaskStrategy.PartialMask).ToList();
            return new FieldRule(
                group.First().Field,
                strategy,
                strategy == FieldMaskStrategy.PartialMask ? partials.Min(static r => r.KeepHead ?? 0) : null,
                strategy == FieldMaskStrategy.PartialMask ? partials.Min(static r => r.KeepTail ?? 0) : null,
                strategy == FieldMaskStrategy.Redact ? group.First(static r => r.Strategy == FieldMaskStrategy.Redact).Replacement : null,
                group.All(static r => r.IsEditable));
        }));
    }

    /// <summary>
    /// 取字段上的有效规则。
    /// </summary>
    /// <remarks>
    /// 字段名不区分大小写。
    /// </remarks>
    /// <returns>有效规则；没有即为空。</returns>
    public FieldRule? Find(string field) => _rules.GetValueOrDefault(field);

    /// <summary>
    /// 字段是否读受保护。
    /// </summary>
    public bool IsReadProtected(string field) => Find(field)?.IsReadProtected == true;

    /// <summary>
    /// 能否拿这个字段做关键字搜索（读受保护的字段不能）。
    /// </summary>
    public bool CanSearch(string field) => !IsReadProtected(field);

    /// <summary>
    /// 文本字段的对外呈现。
    /// </summary>
    public string? Mask(string field, string? value) => Find(field) is { } rule ? rule.Mask(value) : value;

    /// <summary>
    /// 可空的非文本字段的对外呈现：读受保护即为空（非文本只能隐藏）。
    /// </summary>
    public T? Mask<T>(string field, T? value) where T : struct => IsReadProtected(field) ? null : value;

    /// <summary>
    /// 找出请求里第一个按读受保护字段筛选或排序的字段名。
    /// </summary>
    /// <returns>字段名；没有即为空。</returns>
    public string? FirstProtected(PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Filters.Select(static f => f.Field).Concat(request.Sorts.Select(static s => s.Field)).FirstOrDefault(IsReadProtected);
    }

    /// <summary>
    /// 新建时一个字段的处理：只读字段不能填写（空值视为没填）。
    /// </summary>
    public FieldWrite OnCreate(string field, string? value)
        => Find(field) is { IsEditable: false } && !string.IsNullOrEmpty(value) ? FieldWrite.Denied : FieldWrite.Accept;

    /// <summary>
    /// 修改时一个字段的处理。
    /// </summary>
    /// <remarks>
    /// 交回原值、脱敏值或空值视为没改、保留原值；改了只读字段即拒绝；只写字段填了新值照收。
    /// </remarks>
    public FieldWrite OnUpdate(string field, string? original, string? incoming)
    {
        if (Find(field) is not { } rule || (rule.IsEditable && !rule.IsReadProtected))
        {
            return FieldWrite.Accept;
        }

        if (Same(incoming, original) || (rule.IsReadProtected && (Same(incoming, rule.Mask(original)) || string.IsNullOrEmpty(incoming))))
        {
            return FieldWrite.Keep;
        }

        return rule.IsEditable ? FieldWrite.Accept : FieldWrite.Denied;
    }

    private static bool Same(string? left, string? right)
        => string.Equals(string.IsNullOrEmpty(left) ? null : left, string.IsNullOrEmpty(right) ? null : right, StringComparison.Ordinal);

    private static int Strictness(FieldMaskStrategy strategy) => strategy switch
    {
        FieldMaskStrategy.None => 0,
        FieldMaskStrategy.PartialMask => 1,
        FieldMaskStrategy.Hash => 2,
        FieldMaskStrategy.FullMask => 3,
        FieldMaskStrategy.Redact => 4,
        FieldMaskStrategy.Hidden => 5,
        _ => throw new InvalidOperationException($"Unknown mask strategy {strategy}."),
    };
}
