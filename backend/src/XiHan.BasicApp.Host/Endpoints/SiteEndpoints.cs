// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http.Features;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Files;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Settings;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.BasicApp.Settings.Operations.Site;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Host.Endpoints;

/// <summary>
/// 站点设置的图片端点
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><c>POST /api/site/assets/{slot}</c>（multipart，字段 <c>file</c>）：要 <c>settings.site.update</c>，换掉调用方这一侧的 Logo（<c>logo</c>）、浏览器图标（<c>favicon</c>）或登录页背景（<c>login-background</c>），旧图随即回收；回设置页看到的站点设置</item>
/// <item><c>GET /api/site/assets/{id}</c>：匿名读站点图片（登录页在登录之前就要显示），只认经上面的端点上传的文件；文件 ID 不变，按不可变内容缓存一年</item>
/// </list>
/// 按文件头认 PNG、JPEG、WebP 与 ICO（不认 SVG：同源打开会执行脚本），不超过 2 MB；存平台侧、公开级别，不计入租户的存储配额（与头像同一做法）
/// </remarks>
internal static class SiteEndpoints
{
    /// <summary>
    /// 站点图片的大小上限
    /// </summary>
    public const long MaxAssetBytes = 2 * 1024 * 1024;

    /// <summary>
    /// 站点图片在文件库里的标签
    /// </summary>
    public const string AssetTag = "site";

    private const long FormOverhead = 64 * 1024;

    private static readonly (byte[] Head, int Offset, string Extension, string ContentType)[] _kinds =
    [
        ([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], 0, ".png", "image/png"),
        ([0xFF, 0xD8, 0xFF], 0, ".jpg", "image/jpeg"),
        ("WEBP"u8.ToArray(), 8, ".webp", "image/webp"),
        ([0x00, 0x00, 0x01, 0x00], 0, ".ico", "image/x-icon"),
    ];

    /// <summary>
    /// 映射站点图片端点
    /// </summary>
    /// <param name="endpoints">端点构建器</param>
    /// <returns>同一构建器</returns>
    public static IEndpointRouteBuilder MapSiteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost("/api/site/assets/{slot}", UploadAsync)
            .RequireXiHanPermission(SettingsPermissions.SiteUpdate)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<SiteSettingsResponse>()
            .WithName("site.upload-asset")
            .WithSummary("Replace the caller's side's logo, favicon or sign-in background (PNG, JPEG, WebP or ICO, at most 2 MB) with an image uploaded as multipart form data (field `file`).")
            .WithTags("site")
            .AddEndpointFilter(new AuditedEndpointFilter("site.upload-asset", audited: true));
        endpoints.MapGet("/api/site/assets/{id:guid}", ContentAsync)
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .WithName("site.asset")
            .WithSummary("Read a site image anonymously, so the sign-in page can show it before anyone signs in.")
            .WithTags("site")
            .AddEndpointFilter(new AuditedEndpointFilter("site.asset", audited: false));
        return endpoints;
    }

    private static async Task<IResult> UploadAsync(HttpContext http, string slot, FileService files, SiteProfiles profiles)
    {
        var context = ContextOf(http);
        if (slot is not ("logo" or "favicon" or "login-background"))
        {
            return Fail(context, Error.Validation(SettingsErrors.SiteAssetUnknown, SettingsMessages.SiteAssetUnknown, MessageArguments.Of(("field", slot), ("id", string.Empty))));
        }

        if (http.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
        {
            limit.MaxRequestBodySize = MaxAssetBytes + FormOverhead;
        }

        if (!http.Request.HasFormContentType)
        {
            return Fail(context, Invalid());
        }

        IFormCollection form;
        try
        {
            form = await http.Request.ReadFormAsync(new FormOptions { MultipartBodyLengthLimit = MaxAssetBytes + FormOverhead }, http.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is InvalidDataException or BadHttpRequestException)
        {
            return Fail(context, Invalid());
        }

        var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
        if (file is null || file.Length == 0 || file.Length > MaxAssetBytes || await SniffAsync(file, http.RequestAborted).ConfigureAwait(false) is not { } image)
        {
            return Fail(context, Invalid());
        }

        var owner = SettingsScope.OwnerOf(context);
        var current = await profiles.OwnAsync(owner, http.RequestAborted).ConfigureAwait(false);
        if (!current.TryGetValue(out var own))
        {
            return Fail(context, current.Error!);
        }

        var content = file.OpenReadStream();
        await using (content.ConfigureAwait(false))
        {
            var details = new FileDetails(FileAccessLevel.Public, IsTemporary: false, ExpiresAt: null, Tags: AssetTag, Remark: null);
            var uploaded = await files.UploadAsync(context, content, slot + image.Extension, image.ContentType, details, TenantIds.Platform).ConfigureAwait(false);
            if (!uploaded.TryGetValue(out var created))
            {
                return Fail(context, uploaded.Error!);
            }

            var next = slot switch
            {
                "logo" => own with { Logo = created.Id },
                "favicon" => own with { Favicon = created.Id },
                _ => own with { LoginBackground = created.Id },
            };
            var saved = await profiles.SaveAsync(owner, next, http.RequestAborted).ConfigureAwait(false);
            if (!saved.TryGetValue(out var savedSettings))
            {
                return Fail(context, saved.Error!);
            }

            var response = await SiteResponses.SettingsOfAsync(profiles, owner, savedSettings, http.RequestAborted).ConfigureAwait(false);
            return response.TryGetValue(out var settings)
                ? Results.Json(settings, SettingsJsonContext.Default.SiteSettingsResponse)
                : Fail(context, response.Error!);
        }
    }

    private static async Task<IResult> ContentAsync(HttpContext http, Guid id, FileService files)
    {
        var context = ContextOf(http);
        var found = await files.FindAsync(TenantIds.Platform, id, http.RequestAborted).ConfigureAwait(false);
        if (!found.TryGetValue(out var file) || !string.Equals(file.Tags, AssetTag, StringComparison.Ordinal) || file.AccessLevel != FileAccessLevel.Public)
        {
            return Fail(context, found.IsFailure ? found.Error! : Error.NotFound(FilesErrors.FileNotFound, FilesMessages.FileNotFound, MessageArguments.Of(("id", id))));
        }

        var opened = await files.OpenAsync(file, null, http.RequestAborted).ConfigureAwait(false);
        if (!opened.TryGetValue(out var blob))
        {
            return Fail(context, opened.Error!);
        }

        await using (blob.ConfigureAwait(false))
        {
            var response = http.Response;
            response.ContentType = file.ContentType;
            response.ContentLength = blob.Metadata.Length;
            response.Headers.CacheControl = "public, max-age=31536000, immutable";
            response.Headers["X-Content-Type-Options"] = "nosniff";
            await blob.Content.CopyToAsync(response.Body, http.RequestAborted).ConfigureAwait(false);
        }

        return Results.Empty;
    }

    /// <summary>
    /// 按文件头认图片
    /// </summary>
    /// <remarks>
    /// 不信扩展名与上传方给的内容类型；WebP 的标记在第 8 个字节起
    /// </remarks>
    private static async Task<(string Extension, string ContentType)?> SniffAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var head = new byte[16];
        var stream = file.OpenReadStream();
        await using (stream.ConfigureAwait(false))
        {
            var read = await stream.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
            foreach (var (signature, offset, extension, contentType) in _kinds)
            {
                if (read >= offset + signature.Length && head.AsSpan(offset, signature.Length).SequenceEqual(signature) && (offset == 0 || head.AsSpan(0, 4).SequenceEqual("RIFF"u8)))
                {
                    return (extension, contentType);
                }
            }

            return null;
        }
    }

    private static Error Invalid() => Error.Validation(SettingsErrors.SiteAssetInvalid, SettingsMessages.SiteAssetInvalid, MessageArguments.Of(("max", MaxAssetBytes / 1024 / 1024)));

    /// <summary>
    /// 失败：结果交给框架按 Problem Details 写出（响应还没开始写）
    /// </summary>
    private static IResult Fail(InvocationContext context, Error error)
    {
        context.SetOutcome(error);
        return Results.Empty;
    }

    /// <summary>
    /// 端点的调用上下文（端点在框架管道里执行，一定有）
    /// </summary>
    private static InvocationContext ContextOf(HttpContext http)
        => http.FindXiHanContext().TryGetValue(out var context) ? context : throw new InvalidOperationException("The site endpoints must run inside the XiHan pipeline.");
}
