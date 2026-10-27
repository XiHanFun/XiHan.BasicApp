// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Host;
using XiHan.BasicApp.Host.Contracts;

// contracts export --out <dir>：只装配、映射端点并导出接口文档，不启动 Kestrel 与内核应用，不需要数据库
if (ContractsExporter.TryParse(args, out var outputDirectory))
{
    using var exportApp = BasicAppHost.Create(args);
    return ContractsExporter.Export(exportApp, outputDirectory);
}

var app = BasicAppHost.Create(args);
app.Run();
return 0;
