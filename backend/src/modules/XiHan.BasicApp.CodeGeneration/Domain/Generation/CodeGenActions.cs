// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 表配置「包含操作」的规范集合与组合规则
/// </summary>
/// <remarks>
/// 列表与详情是读取基线，始终生成，不在可裁剪之列。
/// 保存表配置与生成代码共用这里的归一化和组合校验，两处不会各认一套。
/// </remarks>
public static class CodeGenActions
{
    /// <summary>新增</summary>
    public const string Create = "create";

    /// <summary>编辑</summary>
    public const string Update = "update";

    /// <summary>删除</summary>
    public const string Delete = "delete";

    /// <summary>导出（本地 CSV 与导出中心）</summary>
    public const string Export = "export";

    /// <summary>导入（CSV 逐行调用新增接口）</summary>
    public const string Import = "import";

    /// <summary>状态切换（按表里的 EnableStatus 状态列启用/停用）</summary>
    public const string Status = "status";

    /// <summary>打印（按页面码取打印模板，打印详情）</summary>
    public const string Print = "print";

    /// <summary>
    /// 可裁剪操作全集（顺序即权限码、按钮在产物里的呈现顺序）
    /// </summary>
    public static readonly IReadOnlyList<string> All = [Create, Update, Delete, Export, Import, Status, Print];

    /// <summary>
    /// 未配置包含操作时的缺省集
    /// </summary>
    /// <remarks>
    /// 状态切换要求表里有状态列，打印要求目标模块接入打印模块，二者不能默认开启，须在表配置里显式勾选。
    /// </remarks>
    public static readonly IReadOnlyList<string> Defaults = [Create, Update, Delete, Export, Import];

    /// <summary>
    /// 归一化包含操作：null/空（未配置）→ 缺省集；非空则按规范集合过滤，未知操作忽略
    /// </summary>
    /// <param name="raw">表配置里逗号分隔的操作串</param>
    /// <returns>按 <see cref="All"/> 顺序排列的操作集合</returns>
    public static IReadOnlyList<string> Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Defaults;
        }

        var selected = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(action => action.ToLowerInvariant())
            .ToHashSet();
        return [.. All.Where(selected.Contains)];
    }

    /// <summary>
    /// 校验操作组合，返回不合规原因（合规时为 null）
    /// </summary>
    /// <remarks>
    /// 导入逐行调用新增接口：只勾导入会产出一个指向不存在接口的导入按钮，前端类型检查都过不了。
    /// </remarks>
    /// <param name="actions">已归一化的操作集合</param>
    public static string? FindConflict(IReadOnlyCollection<string> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);

        return actions.Contains(Import) && !actions.Contains(Create)
            ? "包含操作勾选了导入但没有勾选新增：导入逐行调用新增接口，须同时勾选新增。"
            : null;
    }
}
