// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Workbench;
using XiHan.BasicApp.Identity.Queries;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 身份包给工作台的小组件。
/// </summary>
/// <remarks>
/// 与在线用户页同一口径：当前一侧的有效会话，按数据范围收窄到看得到的用户。
/// </remarks>
/// <param name="sessions">会话查询。</param>
public sealed class IdentityWidgets(SessionQueries sessions) : IWorkbenchWidgetProvider
{
    /// <summary>
    /// 在线用户（此刻持有实时连接的去重用户数）。
    /// </summary>
    public const string OnlineUsers = "identity.online-users";

    /// <summary>
    /// 在线设备（有效会话按设备类型的构成）。
    /// </summary>
    public const string SessionDevices = "identity.session-devices";

    /// <summary>
    /// 没记下设备类型的会话并成一项的键。
    /// </summary>
    private const string Unknown = "unknown";

    /// <inheritdoc />
    public IReadOnlyList<WorkbenchWidget> Widgets { get; } =
    [
        new(OnlineUsers, WorkbenchWidgetKind.Metric, IdentityPermissions.SessionsRead),
        new(SessionDevices, WorkbenchWidgetKind.Chart, IdentityPermissions.SessionsRead),
    ];

    /// <inheritdoc />
    public async ValueTask<Result<WorkbenchWidgetData>> GetAsync(string key, WorkbenchWidgetQuery query, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        switch (key)
        {
            case OnlineUsers:
                var summary = await sessions.SummaryAsync(context).ConfigureAwait(false);
                return new WorkbenchWidgetData(Value: summary.RealtimeOnlineUsers);
            case SessionDevices:
                var active = await sessions.ActiveAsync(context).ConfigureAwait(false);
                var rows = await active
                    .GroupBy(s => s.Device)
                    .Select(s => new { s.Device, Count = SqlFunc.AggregateCount(s.Id) })
                    .ToListAsync(context.CancellationToken).ConfigureAwait(false);
                var slices = rows
                    .GroupBy(static r => string.IsNullOrWhiteSpace(r.Device) ? Unknown : r.Device!, StringComparer.Ordinal)
                    .Select(static g => new WorkbenchSlice(g.Key, g.Sum(static r => (long)r.Count)))
                    .OrderByDescending(static s => s.Value)
                    .ThenBy(static s => s.Key, StringComparer.Ordinal)
                    .ToList();
                return new WorkbenchWidgetData(Slices: slices);
            default:
                throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }
    }
}
