// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XiHan.BasicApp.Contracts.Printing;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Printing.Domain.Services;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Runtime.Schema;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证宿主起不来时没有后台轮询要收尾
/// </summary>
/// <remarks>
/// 内核启动核对失败时宿主不经 StopAsync 直接释放。宿主的后台服务在核对之前就已开始，执行体还没跑起来就被取消时宿主照样记成后台服务故障，
/// 所以后台轮询一律由能力包在内核启动核对通过之后开始，宿主里不登记后台服务
/// </remarks>
public sealed class BackgroundServicesTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task FailedStart_LeavesNoBackgroundLoopsToEnd()
    {
        IReadOnlyList<IHostedService> hosted = [];
        TenantSchemaWatch? watch = null;

        // 打印数据源重复登记：内核启动核对时失败
        await Assert.ThrowsAnyAsync<Exception>(() => BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            DatabaseProviders.Sqlite,
            configure: static builder => builder.Services.AddPrintDataSource(BuiltInPrintDataSources.SystemPrintDemo),
            afterMap: app =>
            {
                hosted = [.. app.Services.GetServices<IHostedService>()];
                watch = app.Services.GetRequiredService<TenantSchemaWatch>();
            }));

        Assert.Empty(hosted.OfType<BackgroundService>().Select(static s => s.GetType().FullName));
        // 独立库核对的轮询没开始过：第一遍核对既没完成也没被取消
        Assert.False(watch!.FirstPass.IsCompleted);
    }
}
