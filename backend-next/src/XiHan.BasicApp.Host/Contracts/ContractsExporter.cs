// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.Framework.Protocols.Http.OpenApi;

namespace XiHan.BasicApp.Host.Contracts;

/// <summary>
/// <c>contracts export</c> 命令：把接口文档与推送方法清单写到目录。
/// </summary>
/// <remarks>
/// <c>contracts export --out &lt;目录&gt;</c>：把宿主装配后的接口文档（<c>openapi.json</c>，含操作、权限码扩展与普通端点）写到目录，不启动应用、不连数据库。
/// 文档输入由框架 <see cref="OpenApiDocumentSource.FromServices"/> 组装，与 <c>MapXiHanOpenApi</c> 服务的 <c>/openapi.json</c> 同源（集成测试逐字节比对）。
/// 同目录另出 <c>realtime.json</c>：Hub 路径与服务端推送的方法名清单（方案 5.11，不是第二份 API 文档）。
/// 前端 <c>gen-contracts</c> 与 CI 的契约漂移门禁读这两个文件。
/// </remarks>
public static class ContractsExporter
{
    /// <summary>
    /// 输出文件名。
    /// </summary>
    public const string FileName = "openapi.json";

    /// <summary>
    /// 推送方法清单的文件名。
    /// </summary>
    public const string RealtimeFileName = "realtime.json";

    /// <summary>
    /// 解析 <c>contracts export</c> 命令行。
    /// </summary>
    /// <remarks>
    /// 写法 <c>contracts export --out &lt;目录&gt;</c>（或 <c>--out=&lt;目录&gt;</c>）。
    /// </remarks>
    /// <returns>是这条命令即为真，不是这条命令返回假。</returns>
    /// <exception cref="ArgumentException">是这条命令但缺 <c>--out</c>（用法错误）。</exception>
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
    /// 生成文档与推送方法清单并写到 <paramref name="outputDirectory"/>。
    /// </summary>
    /// <remarks>
    /// 把完整路径打到 stdout。
    /// </remarks>
    /// <returns>进程退出码。</returns>
    public static int Export(WebApplication app, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        var document = Build(app);
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, FileName);
        File.WriteAllBytes(path, document);
        Console.Out.WriteLine(Path.GetFullPath(path));
        var realtime = Path.Combine(outputDirectory, RealtimeFileName);
        File.WriteAllBytes(realtime, BuildRealtime());
        Console.Out.WriteLine(Path.GetFullPath(realtime));
        return 0;
    }

    /// <summary>
    /// 生成推送方法清单。
    /// </summary>
    /// <remarks>
    /// 格式 <c>{"hubs":[{"path":"/hubs/notification","methods":[...]}]}</c>，方法按字母序，缩进两格、LF 换行。
    /// </remarks>
    public static byte[] BuildRealtime()
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true, NewLine = "\n" }))
        {
            writer.WriteStartObject();
            writer.WriteStartArray("hubs");
            writer.WriteStartObject();
            writer.WriteString("path", RealtimeHubs.Notification);
            writer.WriteStartArray("methods");
            foreach (var method in RealtimeMethods.All.Order(StringComparer.Ordinal))
            {
                writer.WriteStringValue(method);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        buffer.WriteByte((byte)'\n');
        return buffer.ToArray();
    }

    /// <summary>
    /// 生成接口文档的字节。
    /// </summary>
    /// <remarks>
    /// 标题与版本取 <see cref="AppInfo"/>，端点取宿主全部数据源，摘要按默认区域。
    /// </remarks>
    public static byte[] Build(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(static source => source.Endpoints);
        var source = OpenApiDocumentSource.FromServices(app.Services, BasicAppOpenApi.Options(), endpoints);
        return OpenApiDocumentBuilder.Build(source);
    }
}
