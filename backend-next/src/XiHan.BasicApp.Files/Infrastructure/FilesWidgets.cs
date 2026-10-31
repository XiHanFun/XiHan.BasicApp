// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Workbench;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Infrastructure;

/// <summary>
/// 文件包给工作台的小组件。
/// </summary>
/// <remarks>
/// 与文件管理页同一口径：当前一侧未删除的文件，临时文件不算。
/// </remarks>
/// <param name="rows">按侧取行。</param>
public sealed class FilesWidgets(SideRows rows) : IWorkbenchWidgetProvider
{
    /// <summary>
    /// 存储占用（按文件类型与扩展名的字节数）。
    /// </summary>
    public const string StorageMix = "files.storage-mix";

    /// <inheritdoc />
    public IReadOnlyList<WorkbenchWidget> Widgets { get; } =
    [
        new(StorageMix, WorkbenchWidgetKind.Chart, FilesPermissions.FilesRead),
    ];

    /// <inheritdoc />
    /// <remarks>
    /// 构成的键是「文件类型/扩展名」（如 <c>Image/png</c>，没有扩展名时斜杠后为空），值是字节数，前端按类型分组画层级图。
    /// </remarks>
    public async ValueTask<Result<WorkbenchWidgetData>> GetAsync(string key, WorkbenchWidgetQuery query, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!string.Equals(key, StorageMix, StringComparison.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }

        var groups = await rows.Query<SysFile>(SideRows.SideOf(context))
            .Where(f => !f.IsTemporary)
            .GroupBy(f => new { f.Kind, f.Extension })
            .Select(f => new { f.Kind, f.Extension, Bytes = SqlFunc.AggregateSum(f.Size) })
            .ToListAsync(context.CancellationToken).ConfigureAwait(false);
        var slices = groups
            .GroupBy(static g => $"{g.Kind}/{g.Extension?.Trim().TrimStart('.').ToLowerInvariant()}", StringComparer.Ordinal)
            .Select(static g => new WorkbenchSlice(g.Key, g.Sum(static r => r.Bytes)))
            .Where(static s => s.Value > 0)
            .OrderByDescending(static s => s.Value)
            .ThenBy(static s => s.Key, StringComparer.Ordinal)
            .ToList();
        return new WorkbenchWidgetData(Slices: slices);
    }
}
