// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Files;

/// <summary>
/// 业务产出的临时文件（Files 实现）。
/// </summary>
/// <remarks>
/// 给生成产物一类「存一会儿、由业务自己发出去」的文件用（如代码生成的产物包，同导出中心的结果文件）：存在调用方所在的一侧，标为临时、带到期时间，
/// 访问级别是「需要授权」（文件管理照旧要 <c>files.files.read</c>）；读取不再核对通用的文件权限，业务按自己的权限先核对。到期之后读不到。没装 Files 即没有实现。
/// </remarks>
public interface ITemporaryFiles
{
    /// <summary>
    /// 存一份临时文件。
    /// </summary>
    /// <remarks>
    /// 大小上限与存储配额同普通上传。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="source">来源（业务名，如 <c>codegeneration</c>）。</param>
    /// <param name="content">内容。</param>
    /// <param name="fileName">原始文件名。</param>
    /// <param name="contentType">内容类型。</param>
    /// <param name="lifetime">保留多久。</param>
    /// <returns>新文件。</returns>
    Task<Result<AttachmentFile>> SaveAsync(InvocationContext context, string source, Stream content, string fileName, string contentType, TimeSpan lifetime);

    /// <summary>
    /// 打开调用方这一侧的一份临时文件。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="fileId">文件 ID。</param>
    /// <returns>内容（调用方释放）；不在这一侧、不是正常文件或已到期为失败。</returns>
    Task<Result<TemporaryFileContent>> OpenAsync(InvocationContext context, Guid fileId);
}

/// <summary>
/// 临时文件的内容。
/// </summary>
/// <param name="Name">原始文件名。</param>
/// <param name="ContentType">内容类型。</param>
/// <param name="Length">长度（字节）。</param>
/// <param name="Content">内容流。</param>
public sealed record TemporaryFileContent(string Name, string ContentType, long Length, Stream Content) : IAsyncDisposable
{
    /// <inheritdoc />
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
