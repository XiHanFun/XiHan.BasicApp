// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Microsoft.AspNetCore.Http.Features;
using XiHan.BasicApp.Files;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Files.Operations.Files;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.ObjectStorage.Abstractions;
using XiHan.Framework.ObjectStorage.Abstractions.Blobs;
using XiHan.Framework.ObjectStorage.Abstractions.Signing;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Host.Endpoints;

/// <summary>
/// 文件的普通端点。
/// </summary>
/// <remarks>
/// 方案 5.4：只有 JSON 能进操作，上传与下载内容是普通端点，元数据管理是操作。
/// <list type="bullet">
/// <item><c>POST /api/files/upload</c>：multipart 表单上传一个文件（字段 <c>file</c>，可选 <c>accessLevel</c>、<c>isTemporary</c>、<c>expiresAt</c>、<c>tags</c>、<c>remark</c>），要 <c>files.files.create</c>，返回文件。</item>
/// <item><c>GET /api/files/{id}/content</c>：已登录调用方按访问级别读当前一侧的文件内容（接口客户端用），支持单个区间的 <c>Range</c> 请求（206，不可满足 416）。</item>
/// </list>
/// 下载默认按内容类型内联显示，查询 <c>download=true</c> 时以附件下载（带原始文件名）。两者都登记了审计身份，访问日志按 <c>files.upload</c>、<c>files.content</c> 记录。
/// 浏览器的图片、链接与新窗口用 <c>files.presign</c> 换的签名地址：对象存储签出，本地存储的由框架的签名端点（<c>MapXiHanBlobs()</c>，访问日志记 <c>xihan.objectstorage.blob.download</c>）收发。
/// </remarks>
internal static class FileEndpoints
{
    /// <summary>
    /// 表单除文件外的开销上限（字段与分隔）。
    /// </summary>
    private const long FormOverhead = 1024 * 1024;

    /// <summary>
    /// 映射文件端点。
    /// </summary>
    /// <param name="endpoints">端点构建器。</param>
    /// <returns>同一构建器。</returns>
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost("/api/files/upload", UploadAsync)
            .RequireXiHanPermission(FilesPermissions.FilesCreate)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<FileResponse>()
            .WithName("files.upload")
            .WithSummary("Upload one file as multipart form data (field `file`).")
            .WithTags("files")
            .AddEndpointFilter(new AuditedEndpointFilter("files.upload", audited: true));
        endpoints.MapGet("/api/files/{id:guid}/content", ContentAsync)
            .RequireXiHanAuthentication()
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .Produces(StatusCodes.Status206PartialContent, contentType: "application/octet-stream")
            .WithName("files.content")
            .WithSummary("Read a file's content on the caller's side, subject to its access level.")
            .WithTags("files")
            .AddEndpointFilter(new AuditedEndpointFilter("files.content", audited: false));
        return endpoints;
    }

    private static async Task<IResult> UploadAsync(HttpContext http, FileService files, FilesOptions options)
    {
        var context = ContextOf(http);
        // Kestrel 缺省只收 30 MB 的请求体，按文件上限放宽到这个端点
        if (http.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
        {
            limit.MaxRequestBodySize = options.MaxUploadBytes + FormOverhead;
        }

        if (!http.Request.HasFormContentType)
        {
            return Fail(context, Error.Validation(FilesErrors.FileEmpty, FilesMessages.FileEmpty));
        }

        IFormCollection form;
        try
        {
            form = await http.Request.ReadFormAsync(new FormOptions { MultipartBodyLengthLimit = options.MaxUploadBytes + FormOverhead }, http.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is InvalidDataException or BadHttpRequestException)
        {
            return Fail(context, Error.Validation(FilesErrors.FileTooLarge, FilesMessages.FileTooLarge, MessageArguments.Of(("max", options.MaxUploadBytes))));
        }

        var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
        if (file is null || file.Length == 0)
        {
            return Fail(context, Error.Validation(FilesErrors.FileEmpty, FilesMessages.FileEmpty));
        }

        var details = new FileDetails(
            Enum.TryParse<FileAccessLevel>(form["accessLevel"], ignoreCase: true, out var level) && Enum.IsDefined(level) ? level : FileAccessLevel.Authorized,
            bool.TryParse(form["isTemporary"], out var temporary) && temporary,
            DateTimeOffset.TryParse(form["expiresAt"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expires) ? expires : null,
            Clip(form["tags"], 500),
            Clip(form["remark"], 500));
        var content = file.OpenReadStream();
        await using (content.ConfigureAwait(false))
        {
            var uploaded = await files.UploadAsync(context, content, file.FileName, file.ContentType, details).ConfigureAwait(false);
            if (!uploaded.TryGetValue(out var created))
            {
                return Fail(context, uploaded.Error!);
            }

            return Results.Json(FileResponse.From(created), FilesJsonContext.Default.FileResponse);
        }
    }

    private static async Task<IResult> ContentAsync(HttpContext http, Guid id, bool? download, FileService files)
    {
        var context = ContextOf(http);
        var found = await files.FindAsync(SideRows.SideOf(context), id, http.RequestAborted).ConfigureAwait(false);
        if (!found.TryGetValue(out var file))
        {
            return Fail(context, found.Error!);
        }

        var allowed = await files.CheckAccessAsync(context, file).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return Fail(context, allowed.Error!);
        }

        var range = RangeOf(http.Request, file.Size);
        var opened = await files.OpenAsync(file, range, http.RequestAborted).ConfigureAwait(false);
        if (!opened.TryGetValue(out var blob))
        {
            if (opened.Error!.Code == ObjectStorageErrors.RangeNotSatisfiable)
            {
                // 416 自己写出并开始响应：错误仍记为调用结果（访问日志可见），不再被改写成 Problem Details
                http.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
                http.Response.Headers.ContentRange = $"bytes */{file.Size}";
                await http.Response.StartAsync(http.RequestAborted).ConfigureAwait(false);
            }

            return Fail(context, opened.Error!);
        }

        await using (blob.ConfigureAwait(false))
        {
            var response = http.Response;
            response.ContentType = file.ContentType;
            response.Headers.AcceptRanges = "bytes";
            response.Headers.LastModified = blob.Metadata.LastModified.ToString("R", CultureInfo.InvariantCulture);
            if (download == true)
            {
                response.Headers.ContentDisposition = BlobContentDisposition.Attachment(file.OriginalName);
            }

            if (blob.Range is { } served)
            {
                response.StatusCode = StatusCodes.Status206PartialContent;
                response.Headers.ContentRange = $"bytes {served.Offset}-{served.Last}/{served.TotalLength}";
                response.ContentLength = served.Length;
            }
            else
            {
                response.ContentLength = blob.Metadata.Length;
            }

            await blob.Content.CopyToAsync(response.Body, http.RequestAborted).ConfigureAwait(false);
        }

        return Results.Empty;
    }

    /// <summary>
    /// 请求的字节范围。
    /// </summary>
    /// <remarks>
    /// 只认单个 <c>bytes</c> 区间（多区间与不合法的头按整读，RFC 9110 允许忽略）；后缀区间（<c>bytes=-n</c>）按文件大小换算成起点。
    /// </remarks>
    private static BlobRange? RangeOf(HttpRequest request, long size)
    {
        var header = request.GetTypedHeaders().Range;
        if (header is null || !string.Equals(header.Unit.Value, "bytes", StringComparison.OrdinalIgnoreCase) || header.Ranges.Count != 1)
        {
            return null;
        }

        var item = header.Ranges.First();
        return (item.From, item.To) switch
        {
            ({ } from, { } to) => to >= from ? new BlobRange(from, to - from + 1) : null,
            ({ } from, null) => new BlobRange(from),
            (null, { } suffix and > 0) => new BlobRange(Math.Max(0, size - suffix)),
            _ => null,
        };
    }

    /// <summary>
    /// 失败：结果交给框架按 Problem Details 写出（响应还没开始写）。
    /// </summary>
    private static IResult Fail(InvocationContext context, Error error)
    {
        context.SetOutcome(error);
        return Results.Empty;
    }

    /// <summary>
    /// 端点的调用上下文（端点都在框架管道里执行，一定有）。
    /// </summary>
    private static InvocationContext ContextOf(HttpContext http)
        => http.GetXiHanContext().TryGetValue(out var context) ? context : throw new InvalidOperationException("The file endpoints must run inside the XiHan pipeline.");

    private static string? Clip(string? value, int length)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.Length <= length ? trimmed : trimmed[..length];
    }
}
