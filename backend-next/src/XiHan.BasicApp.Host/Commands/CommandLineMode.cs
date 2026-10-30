// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Hosting.CommandLine;
using XiHan.Framework.Localization.Abstractions;

namespace XiHan.BasicApp.Host.Commands;

/// <summary>
/// 命令模式：用同一套装配启动宿主但不监听端口。
/// </summary>
/// <remarks>
/// <c>dotnet run -- &lt;命令&gt; [...]</c> 时用同一套装配启动宿主，但不监听端口，由框架的命令行运行器分发命令（<c>help</c>、<c>graph</c>、<c>pipeline</c>、<c>operations</c>、<c>invoke</c>、<c>seed</c>）。
/// <c>contracts export</c> 不启动应用，另走 <see cref="Contracts.ContractsExporter"/>。
/// </remarks>
public static class CommandLineMode
{
    /// <summary>
    /// 第一个参数是命令名（不以 <c>-</c> 开头、不是 <c>contracts</c>）即命令模式。
    /// </summary>
    public static bool IsCommand(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return args.Count > 0 && !args[0].StartsWith('-') && !string.Equals(args[0], "contracts", StringComparison.Ordinal);
    }

    /// <summary>
    /// 登记框架命令行运行器、内建命令与本应用的命令。
    /// </summary>
    /// <remarks>
    /// 同时登记宿主的消息目录（命令说明与错误文案）。
    /// </remarks>
    public static IServiceCollection AddBasicAppCommands(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddMessageCatalog(HostMessages.Catalog);
        return services.AddXiHanCommandLine().AddXiHanCommand<SeedCommand>().AddXiHanCommand<SchemaCommand>();
    }

    /// <summary>
    /// 命令模式的宿主配置：登记命令，并把服务器换成不监听端口的实现。
    /// </summary>
    public static void Configure(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddBasicAppCommands();
        builder.WebHost.UseServer(new HeadlessServer());
    }
}
