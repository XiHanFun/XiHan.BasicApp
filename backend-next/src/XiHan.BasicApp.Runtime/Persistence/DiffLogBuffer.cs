// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.SqlSugar;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// 一次调用里仓储写入产生的差异日志（作用域服务）：SqlSugar 在写入执行后、提交之前同步回调（ADR-0044），这里只收集；
/// 调用以成功结束时由 Auditing 取走落库，失败的调用已回滚，取走后丢弃。
/// 收集时复制一份：SqlSugar 在分表插入的各组之间复用同一个 <see cref="DiffLogModel"/>，留下引用会被后一组改掉。
/// </summary>
public sealed class DiffLogBuffer
{
    private readonly List<DiffLogModel> _changes = [];

    /// <summary>
    /// 取走已收集的差异并清空。
    /// </summary>
    public IReadOnlyList<DiffLogModel> Drain()
    {
        var drained = _changes.ToList();
        _changes.Clear();
        return drained;
    }

    /// <summary>
    /// 仓储对哪些实体开差异日志：全审计实体（有修改与删除的业务数据），标了 <see cref="DiffLogIgnoredAttribute"/> 的除外。
    /// </summary>
    public static bool IsLogged(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return typeof(AppAuditedEntity).IsAssignableFrom(entityType) && !entityType.IsDefined(typeof(DiffLogIgnoredAttribute), inherit: false);
    }

    /// <summary>
    /// <c>AopEvents.OnDiffLogEvent</c> 的回调：业务数据带着仓储所在的作用域，差异交给那个作用域的缓冲。
    /// </summary>
    public static void OnDiffLog(DiffLogModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.BusinessData is SqlSugarDiffLogData data && data.Services.GetService<DiffLogBuffer>() is { } buffer)
        {
            buffer._changes.Add(new DiffLogModel
            {
                DiffType = model.DiffType,
                BusinessData = data,
                BeforeData = Copy(model.BeforeData),
                AfterData = Copy(model.AfterData),
            });
        }
    }

    private static List<DiffLogTableInfo>? Copy(List<DiffLogTableInfo>? rows)
        => rows?.Select(static row => new DiffLogTableInfo
        {
            TableName = row.TableName,
            TableDescription = row.TableDescription,
            Columns = row.Columns?.Select(static column => new DiffLogColumnInfo
            {
                ColumnName = column.ColumnName,
                ColumnDescription = column.ColumnDescription,
                Value = column.Value,
                IsPrimaryKey = column.IsPrimaryKey,
            }).ToList() ?? [],
        }).ToList();
}
