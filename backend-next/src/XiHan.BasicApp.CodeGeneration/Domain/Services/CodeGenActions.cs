// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Services;

/// <summary>
/// 表配置「包含操作」的规范集合与组合规则。
/// </summary>
/// <remarks>
/// 照 4.x：列表与详情始终生成，不在可裁剪之列；保存表配置与生成代码共用这里的归一化与组合校验。
/// </remarks>
public static class CodeGenActions
{
    /// <summary>
    /// 新建。
    /// </summary>
    public const string Create = "create";

    /// <summary>
    /// 编辑。
    /// </summary>
    public const string Update = "update";

    /// <summary>
    /// 删除。
    /// </summary>
    public const string Delete = "delete";

    /// <summary>
    /// 导出（经导出中心）。
    /// </summary>
    public const string Export = "export";

    /// <summary>
    /// 导入（逐行调新建）。
    /// </summary>
    public const string Import = "import";

    /// <summary>
    /// 启停（要有状态列）。
    /// </summary>
    public const string Status = "status";

    /// <summary>
    /// 打印（经打印模块）。
    /// </summary>
    public const string Print = "print";

    /// <summary>
    /// 可裁剪的全部操作。
    /// </summary>
    /// <remarks>
    /// 顺序即产物里权限码与按钮的顺序。
    /// </remarks>
    public static IReadOnlyList<string> All { get; } = [Create, Update, Delete, Export, Import, Status, Print];

    /// <summary>
    /// 没配置时的缺省集合。
    /// </summary>
    /// <remarks>
    /// 启停要表里有状态列，打印要接入打印模块，都不缺省开启（照 4.x）。
    /// </remarks>
    public static IReadOnlyList<string> Defaults { get; } = [Create, Update, Delete, Export, Import];

    /// <summary>
    /// 归一化包含操作。
    /// </summary>
    /// <param name="raw">逗号分隔的操作；为空即缺省集合，不认得的忽略。</param>
    /// <returns>按 <see cref="All"/> 的顺序排列。</returns>
    public static IReadOnlyList<string> Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Defaults;
        }

        var selected = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(static a => a.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
        return [.. All.Where(selected.Contains)];
    }

    /// <summary>
    /// 组合是否合规。
    /// </summary>
    /// <remarks>
    /// 导入逐行调新建：只勾导入会产出一个指向不存在操作的按钮（照 4.x）。
    /// </remarks>
    /// <param name="actions">归一化之后的操作。</param>
    public static bool IsValid(IReadOnlyCollection<string> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        return !actions.Contains(Import) || actions.Contains(Create);
    }
}
