// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XiHan.BasicApp.Contracts.Printing;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Printing.Domain.Services;
using XiHan.BasicApp.Runtime.Persistence;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证宿主起不来时后台服务的收尾
/// </summary>
/// <remarks>
/// 内核启动核对失败时宿主不经 StopAsync 直接释放，已经开始执行的后台服务随之取消：要正常结束，
/// 以取消或异常结束的会被宿主记成后台服务故障（Error 加 Critical），还要去停一个本来就起不来的宿主
/// </remarks>
public sealed class BackgroundServicesTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task FailedStart_EndsStartedBackgroundServicesNormally()
    {
        IReadOnlyList<BackgroundService> services = [];

        // 打印数据源重复登记：内核启动核对时失败，那时排在内核前面的后台服务都已开始执行
        await Assert.ThrowsAnyAsync<Exception>(() => BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            DatabaseProviders.Sqlite,
            configure: static builder => builder.Services.AddPrintDataSource(BuiltInPrintDataSources.SystemPrintDemo),
            afterMap: app => services = [.. app.Services.GetServices<IHostedService>().OfType<BackgroundService>()]));

        var started = services.Where(static s => s.ExecuteTask is not null).ToList();
        Assert.NotEmpty(started);
        foreach (var service in started)
        {
            await Task.WhenAny(service.ExecuteTask!, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        }

        Assert.Empty(started.Where(static s => !s.ExecuteTask!.IsCompletedSuccessfully).Select(static s => $"{s.GetType().FullName}: {s.ExecuteTask!.Status}"));
    }
}
