// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// 按侧读写租户库实体（文件、导出任务、通知与收件箱）：平台一侧的行以 <c>platform</c> 为租户落在同一张表里（与日志同一做法），框架仓储要求调用带租户，
/// 所以这里直接用工作单元的连接、显式按侧过滤；写之前先开事务，跟操作的其余写入一起提交或回滚。时间与并发戳由这里填（操作人由 AOP 填）。
/// </summary>
/// <param name="unitOfWork">工作单元。</param>
/// <param name="clock">时钟。</param>
public sealed class SideRows(SqlSugarUnitOfWork unitOfWork, TimeProvider clock)
{
    /// <summary>
    /// 调用方所在的一侧：租户主体是本租户，平台主体是 <c>platform</c>。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <returns>侧。</returns>
    public static string SideOf(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetSecurity().Tenant?.Id ?? TenantIds.Platform;
    }

    /// <summary>
    /// 一侧未删除的行。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="side">侧。</param>
    /// <returns>查询。</returns>
    public ISugarQueryable<T> Query<T>(string side)
        where T : TenantScopedAuditedEntity, new()
        => unitOfWork.ConnectionFor(typeof(T)).Queryable<T>().Where(e => e.TenantId == side && !e.IsDeleted);

    /// <summary>
    /// 按 ID 取一侧未删除的行。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="side">侧。</param>
    /// <param name="id">ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>找到的行。</returns>
    public async Task<Maybe<T>> FindAsync<T>(string side, Guid id, CancellationToken cancellationToken)
        where T : TenantScopedAuditedEntity, new()
    {
        var rows = await Query<T>(side).Where(e => e.Id == id).Take(1).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? Maybe.None<T>() : Maybe.Some(rows[0]);
    }

    /// <summary>
    /// 新增。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="row">行（已填 ID 与租户）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task InsertAsync<T>(T row, CancellationToken cancellationToken)
        where T : TenantScopedAuditedEntity, new()
    {
        ArgumentNullException.ThrowIfNull(row);
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        row.CreatedAt = clock.GetUtcNow();
        row.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        await unitOfWork.ConnectionFor(typeof(T)).Insertable(row).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 批量新增（同一类的多行，按 1000 行一批写）。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="rows">行（已填 ID 与租户）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task InsertRangeAsync<T>(IReadOnlyList<T> rows, CancellationToken cancellationToken)
        where T : TenantScopedAuditedEntity, new()
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
        {
            return;
        }

        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        foreach (var row in rows)
        {
            row.CreatedAt = now;
            row.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }

        foreach (var chunk in rows.Chunk(1000))
        {
            await unitOfWork.ConnectionFor(typeof(T)).Insertable(chunk.ToList()).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 整行更新。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="row">行。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task UpdateAsync<T>(T row, CancellationToken cancellationToken)
        where T : TenantScopedAuditedEntity, new()
    {
        ArgumentNullException.ThrowIfNull(row);
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        row.ModifiedAt = clock.GetUtcNow();
        row.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        await unitOfWork.ConnectionFor(typeof(T)).Updateable(row).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 批量整行更新（按 1000 行一批写）。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="rows">行。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task UpdateRangeAsync<T>(IReadOnlyList<T> rows, CancellationToken cancellationToken)
        where T : TenantScopedAuditedEntity, new()
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
        {
            return;
        }

        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        foreach (var row in rows)
        {
            row.ModifiedAt = now;
            row.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }

        foreach (var chunk in rows.Chunk(1000))
        {
            await unitOfWork.ConnectionFor(typeof(T)).Updateable(chunk.ToList()).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 一侧只追加的记录。
    /// </summary>
    /// <typeparam name="T">记录类型。</typeparam>
    /// <param name="side">侧。</param>
    /// <returns>查询。</returns>
    public ISugarQueryable<T> Records<T>(string side)
        where T : TenantScopedRecord, new()
        => unitOfWork.ConnectionFor(typeof(T)).Queryable<T>().Where(e => e.TenantId == side);

    /// <summary>
    /// 追加一条记录。
    /// </summary>
    /// <typeparam name="T">记录类型。</typeparam>
    /// <param name="row">行（已填 ID 与租户）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task AppendAsync<T>(T row, CancellationToken cancellationToken)
        where T : TenantScopedRecord, new()
    {
        ArgumentNullException.ThrowIfNull(row);
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        row.CreatedAt = clock.GetUtcNow();
        row.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        await unitOfWork.ConnectionFor(typeof(T)).Insertable(row).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 软删除。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    /// <param name="row">行。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SoftDeleteAsync<T>(T row, CancellationToken cancellationToken)
        where T : TenantScopedAuditedEntity, new()
    {
        ArgumentNullException.ThrowIfNull(row);
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        row.IsDeleted = true;
        row.DeletedAt = clock.GetUtcNow();
        row.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        await unitOfWork.ConnectionFor(typeof(T)).Updateable(row).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }
}
