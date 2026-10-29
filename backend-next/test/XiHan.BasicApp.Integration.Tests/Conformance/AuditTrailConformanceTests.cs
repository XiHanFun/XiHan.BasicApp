// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.Framework.Auditing.Abstractions.Trail;
using XiHan.Framework.Auditing.Conformance;
using XiHan.Framework.Auditing.Database.Trail;
using XiHan.Framework.Kernel.Diagnostics;

namespace XiHan.BasicApp.Integration.Tests.Conformance;

/// <summary>
/// 操作审计一致性套件的宿主夹具。
/// </summary>
/// <remarks>
/// 框架 <c>Auditing.Database</c> 装在 BasicApp 的 SqlSugar 持久化上。
/// 查询就是宿主里的 <see cref="DatabaseAuditTrail"/>，
/// 接收器写入后立刻刷写（框架套件要求写完即可读，宿主的后台刷写按间隔走）。方案 N2 退出条件：<c>Auditing.Conformance</c> 通过。
/// </remarks>
/// <param name="trail">宿主里的数据库审计。</param>
internal sealed class HostAuditTrailFixture(DatabaseAuditTrail trail) : AuditTrailFixture
{
    /// <inheritdoc />
    public override IAuditSink Sink { get; } = new FlushingSink(trail);

    /// <inheritdoc />
    public override IAuditTrail Trail => trail;

    /// <summary>
    /// 写入即刷写的接收器。
    /// </summary>
    private sealed class FlushingSink(DatabaseAuditTrail trail) : IAuditSink
    {
        /// <inheritdoc />
        public async ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        {
            await trail.WriteAsync(auditEvent, cancellationToken);
            await trail.FlushAsync(cancellationToken);
        }
    }
}

/// <summary>
/// 操作审计（SQLite）。
/// </summary>
public sealed class SqliteAuditTrailConformanceTests : AuditTrailConformance, IAsyncDisposable
{
    private readonly ConformanceHosts _hosts = new(postgres: false);

    /// <inheritdoc />
    protected override async Task<AuditTrailFixture> CreateFixtureAsync()
    {
        var server = await _hosts.StartAsync();
        return new HostAuditTrailFixture(server.Services.GetRequiredService<DatabaseAuditTrail>());
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _hosts.DisposeAsync();
}

/// <summary>
/// 操作审计（PostgreSQL）。
/// </summary>
public sealed class PostgresAuditTrailConformanceTests : AuditTrailConformance, IAsyncDisposable
{
    private readonly ConformanceHosts _hosts = new(postgres: true);

    /// <inheritdoc />
    protected override async Task<AuditTrailFixture> CreateFixtureAsync()
    {
        var server = await _hosts.StartAsync();
        return new HostAuditTrailFixture(server.Services.GetRequiredService<DatabaseAuditTrail>());
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _hosts.DisposeAsync();
}
