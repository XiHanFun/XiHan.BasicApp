// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Workbench;

/// <summary>
/// 一个包给工作台的小组件。
/// </summary>
/// <remarks>
/// 工作台（Workbench）汇总各包登记的提供者：按调用方的权限列出看得到的小组件，再按键转给登记它的提供者取数。
/// 数据只取当前一侧（租户主体是本租户，平台主体是平台一侧），与各包列表页同一口径；
/// 呈现（标题、图标、宽度、图表类型）在前端工作台模块，这里只给数据。
/// </remarks>
public interface IWorkbenchWidgetProvider
{
    /// <summary>
    /// 这个包登记的小组件。
    /// </summary>
    IReadOnlyList<WorkbenchWidget> Widgets { get; }

    /// <summary>
    /// 取一个小组件的数据。
    /// </summary>
    /// <remarks>
    /// 权限已由工作台核过，键一定是 <see cref="Widgets"/> 里的一个。
    /// </remarks>
    /// <param name="key">小组件的键。</param>
    /// <param name="query">取数参数。</param>
    /// <param name="context">当前调用。</param>
    /// <returns>数据。</returns>
    ValueTask<Result<WorkbenchWidgetData>> GetAsync(string key, WorkbenchWidgetQuery query, InvocationContext context);
}

/// <summary>
/// 小组件的取数形态。
/// </summary>
public enum WorkbenchWidgetKind
{
    /// <summary>
    /// 单值指标（今日统计里的一格）。
    /// </summary>
    Metric,

    /// <summary>
    /// 图表。
    /// </summary>
    Chart,

    /// <summary>
    /// 条目列表（如公告）。
    /// </summary>
    List,
}

/// <summary>
/// 一个登记的小组件。
/// </summary>
/// <param name="Key">键，以包名开头，如 <c>auditing.login-trend</c>。</param>
/// <param name="Kind">取数形态。</param>
/// <param name="Permission">看它要的权限码；为空即登录就能看。</param>
public sealed record WorkbenchWidget(string Key, WorkbenchWidgetKind Kind, string? Permission);

/// <summary>
/// 取小组件数据的参数。
/// </summary>
/// <remarks>
/// 「今天」与按日分桶都按查看者的本地日算：本地日的起点换回 UTC 再查。
/// </remarks>
/// <param name="Days">统计最近几天，含今天（趋势、构成类按它取，单值与列表忽略）。</param>
/// <param name="Offset">查看者所在时区相对 UTC 的偏移。</param>
public sealed record WorkbenchWidgetQuery(int Days, TimeSpan Offset)
{
    /// <summary>
    /// 查看者本地今天零点。
    /// </summary>
    /// <param name="now">此刻。</param>
    /// <returns>今天零点（带查看者的偏移）。</returns>
    public DateTimeOffset StartOfToday(DateTimeOffset now) => new(now.ToOffset(Offset).Date, Offset);

    /// <summary>
    /// 统计区间的起点。
    /// </summary>
    /// <remarks>
    /// 即往前数 <see cref="Days"/> 天（含今天）的那天零点。
    /// </remarks>
    /// <param name="now">此刻。</param>
    /// <returns>区间起点（带查看者的偏移）。</returns>
    public DateTimeOffset StartOfRange(DateTimeOffset now) => StartOfToday(now).AddDays(1 - Days);

    /// <summary>
    /// 一个 UTC 时刻在查看者本地的日期与时刻。
    /// </summary>
    /// <param name="utc">UTC 时刻。</param>
    /// <returns>本地时刻。</returns>
    public DateTime Local(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Unspecified) + Offset;
}

/// <summary>
/// 小组件的数据。
/// </summary>
/// <remarks>
/// 按形态只填其中几项：指标填 <paramref name="Value"/>（可带 <paramref name="Previous"/>），图表填序列或构成，列表填条目。
/// </remarks>
/// <param name="Value">指标值。</param>
/// <param name="Previous">上一周期的值（今日对昨日），用来算涨跌；没有为空。</param>
/// <param name="Series">序列。</param>
/// <param name="Slices">构成。</param>
/// <param name="Items">条目。</param>
public sealed record WorkbenchWidgetData(
    long? Value = null,
    long? Previous = null,
    WorkbenchSeries? Series = null,
    IReadOnlyList<WorkbenchSlice>? Slices = null,
    IReadOnlyList<WorkbenchItem>? Items = null);

/// <summary>
/// 同一组类目上的几条序列。
/// </summary>
/// <remarks>
/// 按时间分桶的序列，类目是每小时的起点（ISO 8601、UTC），由前端按查看者的时区合成日或星期与小时。
/// </remarks>
/// <param name="Categories">类目。</param>
/// <param name="Lines">序列，每条的值与类目一一对应。</param>
public sealed record WorkbenchSeries(IReadOnlyList<string> Categories, IReadOnlyList<WorkbenchLine> Lines);

/// <summary>
/// 一条序列。
/// </summary>
/// <param name="Key">序列的键（前端据此取文案）。</param>
/// <param name="Values">值。</param>
public sealed record WorkbenchLine(string Key, IReadOnlyList<long> Values);

/// <summary>
/// 构成里的一份。
/// </summary>
/// <param name="Key">这一份的键：枚举值、错误码或原样的名字（如扩展名、浏览器）。</param>
/// <param name="Value">数量。</param>
/// <param name="Label">显示名（如错误码的说明）；前端按键取文案时为空。</param>
public sealed record WorkbenchSlice(string Key, long Value, string? Label = null);

/// <summary>
/// 列表里的一条。
/// </summary>
/// <param name="Id">标识。</param>
/// <param name="Title">标题。</param>
/// <param name="Kind">类别（前端据此取文案与颜色）；没有为空。</param>
/// <param name="Time">时间；没有为空。</param>
/// <param name="Link">点开去的地址；没有为空。</param>
public sealed record WorkbenchItem(string Id, string Title, string? Kind, DateTimeOffset? Time, string? Link);
