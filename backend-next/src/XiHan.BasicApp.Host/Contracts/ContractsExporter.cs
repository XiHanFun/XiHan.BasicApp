// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using XiHan.BasicApp.Contracts;
using XiHan.Framework.Protocols.Http.OpenApi;

namespace XiHan.BasicApp.Host.Contracts;

/// <summary>
/// <c>contracts export --out &lt;目录&gt;</c>：把宿主装配后的接口文档（<c>openapi.json</c>，含操作、权限码扩展与普通端点）写到目录，不启动应用、不连数据库。
/// 文档输入由框架 <see cref="OpenApiDocumentSource.FromServices"/> 组装，与 <c>MapXiHanOpenApi</c> 服务的 <c>/openapi.json</c> 同源（集成测试逐字节比对）。
/// 前端 <c>gen-contracts</c> 与 CI 的契约漂移门禁读这个文件。
/// </summary>
public static class ContractsExporter
{
    /// <summary>
    /// 输出文件名。
    /// </summary>
    public const string FileName = "openapi.json";

    /// <summary>
    /// 解析命令行：<c>contracts export --out &lt;目录&gt;</c>（或 <c>--out=&lt;目录&gt;</c>）。不是这条命令返回假；是但缺 <c>--out</c> 抛出用法错误。
    /// </summary>
    public static bool TryParse(IReadOnlyList<string> args, [NotNullWhen(true)] out string? outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(args);
        outputDirectory = null;
        if (args.Count < 2 || !string.Equals(args[0], "contracts", StringComparison.Ordinal) || !string.Equals(args[1], "export", StringComparison.Ordinal))
        {
            return false;
        }

        for (var i = 2; i < args.Count; i++)
        {
            if (string.Equals(args[i], "--out", StringComparison.Ordinal) && i + 1 < args.Count)
            {
                outputDirectory = args[i + 1];
                return true;
            }

            if (args[i].StartsWith("--out=", StringComparison.Ordinal))
            {
                outputDirectory = args[i]["--out=".Length..];
                return true;
            }
        }

        throw new ArgumentException("Usage: contracts export --out <directory>");
    }

    /// <summary>
    /// 生成文档并写到 <paramref name="outputDirectory"/>，把完整路径打到 stdout；返回进程退出码。
    /// </summary>
    public static int Export(WebApplication app, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        var document = Build(app);
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, FileName);
        File.WriteAllBytes(path, document);
        Console.Out.WriteLine(Path.GetFullPath(path));
        return 0;
    }

    /// <summary>
    /// 生成文档字节：标题与版本取 <see cref="AppInfo"/>，端点取宿主全部数据源，摘要按默认区域。
    /// </summary>
    public static byte[] Build(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(static source => source.Endpoints);
        var source = OpenApiDocumentSource.FromServices(app.Services, new OpenApiOptions { Title = AppInfo.Name, Version = AppInfo.Version }, endpoints);
        return OpenApiDocumentBuilder.Build(source);
    }
}
