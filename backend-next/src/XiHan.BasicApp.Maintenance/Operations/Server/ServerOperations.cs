// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Maintenance.Operations.Jobs;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Observability.Abstractions;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Maintenance.Operations.Server;

/// <summary>
/// <c>server.get</c> 的请求（没有参数）。
/// </summary>
public sealed record ServerInfoRequest;

/// <summary>
/// 服务器的运行信息。
/// </summary>
/// <remarks>
/// 各项取自框架的 <see cref="IHostMonitor"/>，读不到的字段为空（区分「没有」与「为零」）；CPU 使用率与网卡速率由后台采样，启动后第一个采样间隔内为空。
/// </remarks>
/// <param name="Runtime">运行时与进程。</param>
/// <param name="Processor">处理器。</param>
/// <param name="Memory">内存。</param>
/// <param name="Disks">磁盘。</param>
/// <param name="NetworkInterfaces">网卡。</param>
/// <param name="Gpus">显卡。</param>
/// <param name="Board">主板与 BIOS。</param>
/// <param name="Health">健康项（各 Provider 自带，这里只展示）。</param>
public sealed record ServerInfoResponse(
    RuntimeInfo Runtime,
    ProcessorInfo Processor,
    MemoryInfo Memory,
    IReadOnlyList<DiskInfo> Disks,
    IReadOnlyList<NetworkInterfaceInfo> NetworkInterfaces,
    IReadOnlyList<GpuInfo> Gpus,
    BoardInfo Board,
    IReadOnlyList<HealthItemResponse> Health);

/// <summary>
/// 一个健康项。
/// </summary>
/// <param name="Name">名字。</param>
/// <param name="State">状态。</param>
/// <param name="ErrorCode">出错的错误码；正常为空。</param>
/// <param name="ErrorMessage">出错原因（按区域）；正常为空。</param>
/// <param name="Description">说明；没有为空。</param>
/// <param name="CheckedAt">最近一次检查的时间。</param>
/// <param name="DurationMs">最近一次检查用了多少毫秒。</param>
/// <param name="Data">明细（名字到值，值按不变区域写成文本）。</param>
public sealed record HealthItemResponse(
    string Name,
    LifecycleState State,
    string? ErrorCode,
    string? ErrorMessage,
    string? Description,
    DateTimeOffset? CheckedAt,
    double? DurationMs,
    IReadOnlyDictionary<string, string?> Data);

/// <summary>
/// 取服务器的运行信息与健康项。
/// </summary>
/// <remarks>
/// <c>GET /api/server</c>（平台侧，照 4.x 服务监控页）。运行信息由框架 <see cref="IHostMonitor"/> 给出（不启动外部命令），健康项逐个取各 Provider 的最近一次检查。
/// </remarks>
/// <param name="services">服务提供器（取运行信息与健康项）。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = MaintenancePermissions.ServerRead)]
public sealed class GetServerInfo(IServiceProvider services) : IOperation<ServerInfoRequest, ServerInfoResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "server.get";

    /// <inheritdoc />
    public ValueTask<Result<ServerInfoResponse>> HandleAsync(ServerInfoRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return ValueTask.FromResult(Result.Failure<ServerInfoResponse>(denied));
        }

        if (services.GetService<IHostMonitor>() is not { } monitor)
        {
            return ValueTask.FromResult(Result.Failure<ServerInfoResponse>(Error.Unavailable(MaintenanceErrors.HostMonitorUnavailable, MaintenanceMessages.HostMonitorUnavailable)));
        }

        var health = services.GetServices<IHealthContributor>()
            .SelectMany(static contributor => contributor.Check())
            .OrderBy(static status => status.Name, StringComparer.Ordinal)
            .Select(status => new HealthItemResponse(
                status.Name,
                status.State,
                status.Error?.Code,
                status.Error is { } error ? context.Localize(error) : null,
                status.Description,
                status.CheckedAt,
                status.Duration?.TotalMilliseconds,
                status.Data.ToDictionary(static d => d.Key, static d => d.Value is null ? null : Convert.ToString(d.Value, CultureInfo.InvariantCulture), StringComparer.Ordinal)))
            .ToList();
        var response = new ServerInfoResponse(monitor.ReadRuntime(), monitor.ReadProcessor(), monitor.ReadMemory(), monitor.ReadDisks(), monitor.ReadNetworkInterfaces(), monitor.ReadGpus(), monitor.ReadBoard(), health);
        return ValueTask.FromResult(Result.Success(response));
    }
}
