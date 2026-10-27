// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;

namespace XiHan.BasicApp.Host.Commands;

/// <summary>
/// 命令模式的服务器：不监听任何端口。宿主仍按完整装配启动（数据库、特性、内核），命令经内核管道执行，但不与正在运行的应用实例争抢端口。
/// </summary>
internal sealed class HeadlessServer : IServer
{
    /// <inheritdoc />
    public IFeatureCollection Features { get; } = new FeatureCollection();

    /// <inheritdoc />
    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        where TContext : notnull
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
