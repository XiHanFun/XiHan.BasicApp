// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Domain.Entities;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// 操作人戳：在调用链上传递当前调用的主体 ID。
/// </summary>
/// <remarks>
/// 当前调用的主体 ID 经异步本地值在调用链上流动，SqlSugar 写库回调据此填 <see cref="IHasCreator"/> / <see cref="IHasModifier"/> / <see cref="IHasDeleter"/>。
/// </remarks>
public static class AuditorStamp
{
    private static readonly AsyncLocal<string?> _actor = new();

    /// <summary>
    /// 当前主体 ID。
    /// </summary>
    /// <remarks>
    /// 不在调用里或主体未认证时为 <see langword="null"/>。
    /// </remarks>
    public static string? Actor => _actor.Value;

    /// <summary>
    /// 进入一段以指定主体为操作人的范围。
    /// </summary>
    /// <returns>范围句柄，释放即还原之前的值。</returns>
    public static IDisposable Enter(string? actor)
    {
        var previous = _actor.Value;
        _actor.Value = actor;
        return new Scope(previous);
    }

    /// <summary>
    /// 写库回调：按操作类型与列名填操作人。
    /// </summary>
    /// <remarks>
    /// SqlSugar <c>AopEvents.DataExecuting</c> 的回调；没有主体时不动字段（系统作业写入保持 <see langword="null"/>）。
    /// </remarks>
    /// <param name="value">列的当前值。</param>
    /// <param name="column">列与实体信息。</param>
    public static void OnDataExecuting(object value, DataFilterModel column)
    {
        ArgumentNullException.ThrowIfNull(column);
        var actor = _actor.Value;
        if (actor is null)
        {
            return;
        }

        switch (column.OperationType)
        {
            case DataFilterType.InsertByObject when column.EntityValue is IHasCreator && string.Equals(column.PropertyName, nameof(IHasCreator.CreatedBy), StringComparison.Ordinal):
                column.SetValue(actor);
                break;
            case DataFilterType.UpdateByObject when column.EntityValue is IHasModifier && string.Equals(column.PropertyName, nameof(IHasModifier.ModifiedBy), StringComparison.Ordinal):
                column.SetValue(actor);
                break;
            case DataFilterType.UpdateByObject when column.EntityValue is IHasDeleter and ISoftDeletable { IsDeleted: true } && string.Equals(column.PropertyName, nameof(IHasDeleter.DeletedBy), StringComparison.Ordinal) && value is null:
                // 软删除是整行写回：删除标记已置位而删除人还空着的那一次写入，就是删除本身
                column.SetValue(actor);
                break;
            default:
                break;
        }
    }

    private sealed class Scope(string? previous) : IDisposable
    {
        public void Dispose() => _actor.Value = previous;
    }
}
