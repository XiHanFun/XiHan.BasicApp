// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Host;
using XiHan.BasicApp.Host.Commands;
using XiHan.BasicApp.Host.Contracts;
using XiHan.Framework.Hosting.CommandLine;

// contracts export：构建宿主、导出接口文档，不启动应用、不连数据库
if (ContractsExporter.TryParse(args, out var outputDirectory))
{
    using var exportApp = BasicAppHost.Create(args);
    return ContractsExporter.Export(exportApp, outputDirectory);
}

// 命令模式：同一套装配，不监听端口；命令经完整内核管道，日志只写 stderr，stdout 只承载命令输出
if (CommandLineMode.IsCommand(args))
{
    var commandApp = BasicAppHost.Create(args, CommandLineMode.Configure);
    return await commandApp.RunXiHanCommandLineAsync(args);
}

var app = BasicAppHost.Create(args);
app.Run();
return 0;
