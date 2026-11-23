// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using CrmPlugin;

// 进程外插件（协议 xihan.plugin.v1）：以 gRPC 服务端提供 Plugin 服务，向 stdout 写一行握手行公布端点，日志只走 stderr；
// stdin 关闭时收尾退出，宿主退出或崩溃都不留下孤儿进程
var endpoint = PluginListener.Create();
var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions { Args = args, ApplicationName = "CrmPlugin" });
builder.WebHost.UseKestrelCore();
if (OperatingSystem.IsWindows())
{
    // 命名管道只允许当前用户连接
    builder.WebHost.UseNamedPipes(static pipes => pipes.CurrentUserOnly = true);
}

builder.WebHost.ConfigureKestrel(endpoint.Listen);
builder.Services.AddLogging(static logging => logging.AddConsole(static console => console.LogToStandardErrorThreshold = LogLevel.Trace));
builder.Services.AddRoutingCore();
// 只接受带本次会话令牌的调用
builder.Services.AddGrpc(static grpc => grpc.Interceptors.Add<SessionGuard>());
builder.Services.AddSingleton<PluginState>();
builder.Services.AddSingleton<CustomerStore>();

var app = builder.Build();
app.Lifetime.ApplicationStopped.Register(endpoint.Delete);
app.MapGrpcService<CrmPluginService>();
await app.StartAsync().ConfigureAwait(false);
endpoint.Restrict();

// 握手行：宿主读到后才连接
await Console.Out.WriteLineAsync(endpoint.HandshakeLine).ConfigureAwait(false);
await Console.Out.FlushAsync().ConfigureAwait(false);

_ = Task.Run(async () =>
{
    try
    {
        while (await Console.In.ReadLineAsync().ConfigureAwait(false) is not null)
        {
        }
    }
    catch (IOException)
    {
        // 读出错同读到结尾
    }

    app.Lifetime.StopApplication();
});

await app.WaitForShutdownAsync().ConfigureAwait(false);
