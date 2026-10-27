// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using XiHan.BasicApp.Contracts;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions.Locales;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;
using XiHan.Framework.Protocols.Http;
using XiHan.Framework.Protocols.Http.OpenApi;
using XiHan.Framework.Security.Abstractions.Authentication;

namespace XiHan.BasicApp.Host.Contracts;

/// <summary>
/// <c>contracts export --out &lt;dir&gt;</c>：把与 <c>/openapi.json</c> 同一生成器产出的接口文档写成 <c>openapi.json</c>，供前端 <c>gen-contracts</c> 与 CI 漂移门禁使用。
/// 摘要按默认区域（<c>en-US</c>）取，与 <c>/openapi.json</c> 在 <c>Accept-Language: en-US</c> 下的内容一致。不启动应用，不需要数据库。
/// </summary>
public static class ContractsExporter
{
    /// <summary>导出文件名。</summary>
    public const string FileName = "openapi.json";

    private const string DescriptionKeySuffix = ".description";

    /// <summary>
    /// 识别命令行 <c>contracts export --out &lt;dir&gt;</c>（也接受 <c>--out=&lt;dir&gt;</c>）。
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
    /// 生成文档并写入 <paramref name="outputDirectory"/>/<see cref="FileName"/>；返回进程退出码。
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
    /// 与 <c>MapXiHanOpenApi</c> 同一来源的文档字节：登记表、权限码注册表、认证方案与宿主已映射的全部端点。
    /// </summary>
    public static byte[] Build(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var services = app.Services;
        var localizer = services.GetService<IMessageLocalizer>();
        var locale = localizer?.Options.DefaultLocale ?? SupportedLocales.Default;
        return OpenApiDocumentBuilder.Build(new OpenApiDocumentSource(
            services.GetRequiredService<OperationRegistry>(),
            services.GetService<HttpProtocolOptions>() ?? new HttpProtocolOptions(),
            services.GetService<PermissionRegistry>())
        {
            Title = AppInfo.Name,
            Version = AppInfo.Version,
            AuthenticationSchemes = services.GetServices<IAuthenticationHandler>().Select(static h => h.Scheme).ToList(),
            Endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(static source => source.Endpoints).ToList(),
            Describe = localizer is null ? null : id => Describe(localizer, id, locale),
        });
    }

    private static string? Describe(IMessageLocalizer localizer, string operationId, string locale)
    {
        var key = operationId.Replace('-', '_') + DescriptionKeySuffix;
        return MessageTemplate.IsValidKey(key) && localizer.Find(key, locale).TryGetValue(out var text) ? text : null;
    }
}
