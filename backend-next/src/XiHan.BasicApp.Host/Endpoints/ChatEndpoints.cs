// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat;
using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Chat.Operations.ChatMessages;
using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Files;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;

namespace XiHan.BasicApp.Host.Endpoints;

/// <summary>
/// 聊天的普通端点。
/// </summary>
/// <remarks>
/// <c>POST /api/chat-messages/attachments</c>：multipart 表单上传一个聊天附件（字段 <c>file</c>），只要聊天的发送权限（方案 H2，4.x 要通用文件的新建权限），
/// 文件经 Files 的附件契约存在调用方这一侧、来源记 <c>chat</c>，发送时核对是自己上传的；大小上限与存储配额同普通上传。访问日志按 <c>chat-messages.upload</c> 记录。
/// 只在聊天模块开着时映射。
/// </remarks>
internal static class ChatEndpoints
{
    /// <summary>
    /// 映射聊天端点。
    /// </summary>
    /// <param name="endpoints">端点构建器。</param>
    /// <returns>同一构建器。</returns>
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost("/api/chat-messages/attachments", UploadAsync)
            .RequireXiHanPermission(ChatPermissions.ChatMessagesSend)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ChatAttachmentUploadResponse>()
            .WithName("chat-messages.upload")
            .WithSummary("Upload one chat attachment as multipart form data (field `file`).")
            .WithTags("chat-messages")
            .AddEndpointFilter(new AuditedEndpointFilter("chat-messages.upload", audited: true));
        return endpoints;
    }

    private static async Task<IResult> UploadAsync(HttpContext http, IAttachmentFiles attachments, FilesOptions options)
    {
        var context = FileEndpoints.ContextOf(http);
        var read = await FileEndpoints.ReadFormFileAsync(http, options).ConfigureAwait(false);
        if (!read.TryGetValue(out var upload))
        {
            return FileEndpoints.Fail(context, read.Error!);
        }

        var content = upload.File.OpenReadStream();
        await using (content.ConfigureAwait(false))
        {
            var uploaded = await attachments.UploadAsync(context, ChatRules.AttachmentSource, content, upload.File.FileName, upload.File.ContentType).ConfigureAwait(false);
            if (!uploaded.TryGetValue(out var file))
            {
                return FileEndpoints.Fail(context, uploaded.Error!);
            }

            return Results.Json(new ChatAttachmentUploadResponse(new ChatAttachmentResponse(file.Id, file.Name, file.Size, file.ContentType)), ChatJsonContext.Default.ChatAttachmentUploadResponse);
        }
    }
}
