// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http.Features;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Operations.Profile;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Host.Endpoints;

/// <summary>
/// 个人中心的头像上传端点。
/// </summary>
/// <remarks>
/// <c>POST /api/profile/avatar</c>（multipart，字段 <c>file</c>）。只要求登录，不要 Files 的上传权限；模仿会话不拒绝（4.x 同，资料不拦）。
/// 按文件头认 JPEG / PNG（不信扩展名与上传方给的内容类型），不超过 2 MB；存平台侧、公开级别（6.0 的用户跨租户，头像跟着用户走、不属于某个租户，所以不计入租户的存储配额），
/// 随即换成本人的头像并回收旧的头像文件，回本人的资料。去掉头像走 <c>profile.remove-avatar</c>。
/// </remarks>
internal static class ProfileEndpoints
{
    /// <summary>
    /// 头像的大小上限。
    /// </summary>
    /// <remarks>
    /// 4.x 同。
    /// </remarks>
    public const long MaxAvatarBytes = 2 * 1024 * 1024;

    private const long FormOverhead = 64 * 1024;

    private static readonly byte[] _jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// 映射头像端点。
    /// </summary>
    /// <param name="endpoints">端点构建器。</param>
    /// <returns>同一构建器。</returns>
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost("/api/profile/avatar", UploadAvatarAsync)
            .RequireXiHanAuthentication()
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ProfileResponse>()
            .WithName("profile.upload-avatar")
            .WithSummary("Upload the signed-in user's avatar (JPEG or PNG, at most 2 MB) as multipart form data (field `file`).")
            .WithTags("profile")
            .AddEndpointFilter(new AuditedEndpointFilter("profile.upload-avatar", audited: true));
        return endpoints;
    }

    private static async Task<IResult> UploadAvatarAsync(HttpContext http, FileService files, ProfileAvatars avatars)
    {
        var context = ContextOf(http);
        var caller = await avatars.CallerAsync(context).ConfigureAwait(false);
        if (!caller.TryGetValue(out var found))
        {
            return Fail(context, caller.Error!);
        }

        if (http.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
        {
            limit.MaxRequestBodySize = MaxAvatarBytes + FormOverhead;
        }

        if (!http.Request.HasFormContentType)
        {
            return Fail(context, Invalid());
        }

        IFormCollection form;
        try
        {
            form = await http.Request.ReadFormAsync(new FormOptions { MultipartBodyLengthLimit = MaxAvatarBytes + FormOverhead }, http.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is InvalidDataException or BadHttpRequestException)
        {
            return Fail(context, Invalid());
        }

        var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
        if (file is null || file.Length == 0 || file.Length > MaxAvatarBytes)
        {
            return Fail(context, Invalid());
        }

        var kind = await SniffAsync(file, http.RequestAborted).ConfigureAwait(false);
        if (kind is not { } image)
        {
            return Fail(context, Invalid());
        }

        var content = file.OpenReadStream();
        await using (content.ConfigureAwait(false))
        {
            var details = new FileDetails(FileAccessLevel.Public, IsTemporary: false, ExpiresAt: null, Tags: "avatar", Remark: null);
            var uploaded = await files.UploadAsync(context, content, "avatar" + image.Extension, image.ContentType, details, TenantIds.Platform).ConfigureAwait(false);
            if (!uploaded.TryGetValue(out var created))
            {
                return Fail(context, uploaded.Error!);
            }

            var set = await avatars.SetAsync(found.User, created.Id, http.RequestAborted).ConfigureAwait(false);
            return set.IsFailure
                ? Fail(context, set.Error!)
                : Results.Json(ProfileResponse.From(found.User, found.Security), IdentityJsonContext.Default.ProfileResponse);
        }
    }

    /// <summary>
    /// 按文件头认图片。
    /// </summary>
    /// <remarks>
    /// JPEG 以 <c>FF D8 FF</c> 开头，PNG 以 8 字节签名开头；别的一律不认。
    /// </remarks>
    private static async Task<(string Extension, string ContentType)?> SniffAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var head = new byte[_png.Length];
        var stream = file.OpenReadStream();
        await using (stream.ConfigureAwait(false))
        {
            var read = await stream.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
            if (read >= _png.Length && head.AsSpan().SequenceEqual(_png))
            {
                return (".png", "image/png");
            }

            return read >= _jpeg.Length && head.AsSpan(0, _jpeg.Length).SequenceEqual(_jpeg) ? (".jpg", "image/jpeg") : null;
        }
    }

    private static Error Invalid() => Error.Validation(IdentityErrors.AvatarInvalid, IdentityMessages.AvatarInvalid, MessageArguments.Of(("max", MaxAvatarBytes / 1024 / 1024)));

    /// <summary>
    /// 失败：结果交给框架按 Problem Details 写出（响应还没开始写）。
    /// </summary>
    private static IResult Fail(InvocationContext context, Error error)
    {
        context.SetOutcome(error);
        return Results.Empty;
    }

    /// <summary>
    /// 端点的调用上下文（端点在框架管道里执行，一定有）。
    /// </summary>
    private static InvocationContext ContextOf(HttpContext http)
        => http.GetXiHanContext().TryGetValue(out var context) ? context : throw new InvalidOperationException("The profile endpoints must run inside the XiHan pipeline.");
}
