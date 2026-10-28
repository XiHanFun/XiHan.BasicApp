// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Domain.Services;

/// <summary>
/// 分表日志的写入器（单例）：登记只是进有界队列，不阻塞调用、不加入调用的事务，满了丢弃并计数；后台循环攒够一批或到刷写间隔就落库。
/// 落库按「日志类型 × 租户」分组，每组在自己的作用域与带该租户的工作单元里经仓储批量写入（框架按 <c>[SplitTable]</c> 落到各月的表）；一组失败只记日志、丢弃该组并计数，不重试。
/// 与框架 <c>Auditing.Database</c> 对操作审计的做法一致（ADR-0028）：审计存储不可用时丢日志而不是拒服务，丢了多少可观测。
/// </summary>
public sealed partial class AuditLogWriter : IDisposable
{
    /// <summary>
    /// 丢弃警告的节流窗口：窗口内只记一条，附上累计丢弃数。
    /// </summary>
    private static readonly TimeSpan _dropWarningInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopes;
    private readonly AuditLogOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger _logger;

    /// <summary>
    /// 等待落库的日志；Wait 模式下 TryWrite 不阻塞、满了返回 false，丢弃才能被计数。
    /// </summary>
    private readonly Channel<AuditLogRecord> _queue;

    /// <summary>
    /// 批满信号：攒够一批时唤醒后台循环，不等刷写间隔；容量 1，重复信号丢弃。
    /// </summary>
    private readonly Channel<bool> _wake = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    /// <summary>
    /// 刷写互斥：后台循环与 <see cref="FlushAsync"/> 不同时出队。
    /// </summary>
    private readonly SemaphoreSlim _flushing = new(1, 1);

    private long _dropped;
    private long _failed;
    private long _persisted;
    private long _lastDropWarningTicks;

    /// <summary>
    /// 创建。
    /// </summary>
    public AuditLogWriter(IServiceScopeFactory scopes, AuditLogOptions options, TimeProvider clock, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = loggerFactory.CreateLogger<AuditLogWriter>();
        _queue = Channel.CreateBounded<AuditLogRecord>(new BoundedChannelOptions(options.QueueCapacity) { FullMode = BoundedChannelFullMode.Wait });
    }

    /// <summary>
    /// 因队列满或已停止而丢弃的日志数。
    /// </summary>
    public long DroppedCount => Interlocked.Read(ref _dropped);

    /// <summary>
    /// 落库失败的日志数。
    /// </summary>
    public long FailedCount => Interlocked.Read(ref _failed);

    /// <summary>
    /// 已落库的日志数。
    /// </summary>
    public long PersistedCount => Interlocked.Read(ref _persisted);

    /// <summary>
    /// 登记一条日志；<c>Id</c> 与 <c>TenantId</c> 由调用方填好（平台一侧是 <c>platform</c>）。队列满了或已停止返回 <see langword="false"/>。
    /// </summary>
    public bool Enqueue(AuditLogRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!_queue.Writer.TryWrite(record))
        {
            var dropped = Interlocked.Increment(ref _dropped);
            var now = _clock.GetUtcNow().UtcTicks;
            var last = Interlocked.Read(ref _lastDropWarningTicks);
            if (now - last >= _dropWarningInterval.Ticks && Interlocked.CompareExchange(ref _lastDropWarningTicks, now, last) == last)
            {
                LogDropped(_logger, dropped);
            }

            return false;
        }

        if (_queue.Reader.Count >= _options.BatchSize)
        {
            _wake.Writer.TryWrite(true);
        }

        return true;
    }

    /// <summary>
    /// 后台刷写循环：有日志就等到攒够一批或到刷写间隔，再整批落库；<paramref name="stopping"/> 取消即退出（剩余的由 <see cref="FlushAsync"/> 收尾）。
    /// </summary>
    public async Task RunAsync(CancellationToken stopping)
    {
        try
        {
            while (await _queue.Reader.WaitToReadAsync(stopping).ConfigureAwait(false))
            {
                if (_queue.Reader.Count < _options.BatchSize)
                {
                    using var waiting = CancellationTokenSource.CreateLinkedTokenSource(stopping);
                    var interval = Task.Delay(_options.FlushInterval, _clock, waiting.Token);
                    var full = _wake.Reader.ReadAsync(waiting.Token).AsTask();
                    await Task.WhenAny(interval, full).ConfigureAwait(false);
                    await waiting.CancelAsync().ConfigureAwait(false);
                    stopping.ThrowIfCancellationRequested();
                }

                await FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            // 停止信号：退出循环，队列里剩余的由停止流程刷完
        }
    }

    /// <summary>
    /// 停止接收；之后到达的日志按丢弃计数。
    /// </summary>
    public void Complete() => _queue.Writer.TryComplete();

    /// <summary>
    /// 立即把队列里现有的日志全部落库并等待完成；与后台循环互斥。
    /// </summary>
    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        await _flushing.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var batch = new List<AuditLogRecord>(_options.BatchSize);
            while (_queue.Reader.TryRead(out var record))
            {
                batch.Add(record);
                if (batch.Count >= _options.BatchSize)
                {
                    await WriteAsync(batch).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await WriteAsync(batch).ConfigureAwait(false);
            }
        }
        finally
        {
            _flushing.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _flushing.Dispose();

    /// <summary>
    /// 按类型与租户分组落库；落库不受调用方取消影响（日志已出队，取消即丢失）。
    /// </summary>
    private async Task WriteAsync(IReadOnlyList<AuditLogRecord> batch)
    {
        foreach (var group in batch.GroupBy(static r => (Type: r.GetType(), r.TenantId)))
        {
            var records = group.ToList();
            try
            {
                var written = await WriteGroupAsync(group.Key.Type, group.Key.TenantId, records).ConfigureAwait(false);
                if (written.IsSuccess)
                {
                    Interlocked.Add(ref _persisted, records.Count);
                }
                else
                {
                    Interlocked.Add(ref _failed, records.Count);
                    LogRejected(_logger, records.Count, group.Key.Type.Name, group.Key.TenantId, written.Error!.Code);
                }
            }
            catch (Exception exception)
            {
                Interlocked.Add(ref _failed, records.Count);
                LogWriteFailed(_logger, exception, records.Count, group.Key.Type.Name, group.Key.TenantId);
            }
        }
    }

    private async Task<Result> WriteGroupAsync(Type type, string tenantId, IReadOnlyList<AuditLogRecord> records)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginAsync(new UnitOfWorkOptions(TenantId: tenantId), CancellationToken.None).ConfigureAwait(false);
        try
        {
            var results = type switch
            {
                _ when type == typeof(SysAccessLog) => await AddAsync<SysAccessLog>(scope.ServiceProvider, records).ConfigureAwait(false),
                _ when type == typeof(SysExceptionLog) => await AddAsync<SysExceptionLog>(scope.ServiceProvider, records).ConfigureAwait(false),
                _ when type == typeof(SysLoginLog) => await AddAsync<SysLoginLog>(scope.ServiceProvider, records).ConfigureAwait(false),
                _ when type == typeof(SysDiffLog) => await AddAsync<SysDiffLog>(scope.ServiceProvider, records).ConfigureAwait(false),
                _ when type == typeof(SysPermissionChangeLog) => await AddAsync<SysPermissionChangeLog>(scope.ServiceProvider, records).ConfigureAwait(false),
                _ when type == typeof(SysConditionHit) => await AddAsync<SysConditionHit>(scope.ServiceProvider, records).ConfigureAwait(false),
                _ when type == typeof(SysOpenApiLog) => await AddAsync<SysOpenApiLog>(scope.ServiceProvider, records).ConfigureAwait(false),
                _ => throw new InvalidOperationException($"'{type.FullName}' is not an audit log entity."),
            };
            foreach (var result in results)
            {
                if (result.IsFailure)
                {
                    await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    return result;
                }
            }

            return await unitOfWork.CommitAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            if (unitOfWork.State == UnitOfWorkState.Active)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }

            throw;
        }
    }

    private static Task<IReadOnlyList<Result>> AddAsync<TEntity>(IServiceProvider services, IReadOnlyList<AuditLogRecord> records)
        where TEntity : AuditLogRecord
        => services.GetRequiredService<IRepository<TEntity, Guid>>().AddRangeAsync(records.Cast<TEntity>().ToList(), CancellationToken.None);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The audit log queue is full; {Dropped} logs have been dropped so far.")]
    private static partial void LogDropped(ILogger logger, long dropped);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Count} {Type} records of tenant '{TenantId}' were rejected: {Code}.")]
    private static partial void LogRejected(ILogger logger, int count, string type, string tenantId, string code);

    [LoggerMessage(Level = LogLevel.Error, Message = "Writing {Count} {Type} records of tenant '{TenantId}' failed.")]
    private static partial void LogWriteFailed(ILogger logger, Exception exception, int count, string type, string tenantId);
}
