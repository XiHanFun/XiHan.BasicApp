// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration;
using XiHan.BasicApp.CodeGeneration.Queries;
using XiHan.BasicApp.Contracts.Files;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;
using XiHan.Framework.ObjectStorage.Abstractions.Signing;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Host.Endpoints;

/// <summary>
/// 代码生成的普通端点
/// </summary>
/// <remarks>
/// <c>GET /api/codegen-histories/{id}/package</c>：以附件下载一次生成的产物包（Zip），要生成历史的读权限（方案 D1 列出的普通端点）
/// 产物包是 Files 的临时文件（7 天），经临时文件契约读出，不再要通用的文件权限；写进项目、生成失败或已到期的是 <c>CODEGENERATION_NOTFOUND_005</c>
/// 4.x 把产物包以 Base64 放进生成的响应，6.0 改为生成之后从这里下载。访问日志按 <c>codegen-histories.package</c> 记录。只在代码生成模块开着时映射
/// </remarks>
internal static class CodeGenerationEndpoints
{
    /// <summary>
    /// 映射代码生成端点
    /// </summary>
    /// <param name="endpoints">端点构建器</param>
    /// <returns>同一构建器</returns>
    public static IEndpointRouteBuilder MapCodeGenerationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/api/codegen-histories/{id:guid}/package", PackageAsync)
            .RequireXiHanPermission(CodeGenerationPermissions.HistoriesRead)
            .Produces(StatusCodes.Status200OK, contentType: "application/zip")
            .WithName("codegen-histories.package")
            .WithSummary("Download the generated package of a code generation history entry as a zip file.")
            .WithTags("codegen-histories")
            .AddEndpointFilter(new AuditedEndpointFilter("codegen-histories.package", audited: false));
        return endpoints;
    }

    private static async Task<IResult> PackageAsync(HttpContext http, Guid id, CodegenHistoryQueries histories, ITemporaryFiles files)
    {
        var context = FileEndpoints.ContextOf(http);
        var found = await histories.FindAsync(id, context).ConfigureAwait(false);
        if (!found.TryGetValue(out var history))
        {
            return FileEndpoints.Fail(context, found.Error!);
        }

        var missing = Error.NotFound(CodeGenerationErrors.PackageNotFound, CodeGenerationMessages.HistoryPackageNotFound, MessageArguments.Of(("id", id)));
        if (history.FileId is not { } fileId)
        {
            return FileEndpoints.Fail(context, missing);
        }

        var opened = await files.OpenAsync(context, fileId).ConfigureAwait(false);
        if (!opened.TryGetValue(out var package))
        {
            // 临时文件到期或被清理：说成这次生成的产物包没有了，而不是一个用户看不到的文件 ID
            return FileEndpoints.Fail(context, opened.Error!.Kind == "NOTFOUND" ? missing : opened.Error!);
        }

        await using (package.ConfigureAwait(false))
        {
            var response = http.Response;
            response.ContentType = "application/zip";
            response.ContentLength = package.Length;
            response.Headers.ContentDisposition = BlobContentDisposition.Attachment(package.Name);
            await package.Content.CopyToAsync(response.Body, http.RequestAborted).ConfigureAwait(false);
        }

        return Results.Empty;
    }
}
