// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Files;

/// <summary>
/// 业务附件的文件（Files 实现）
/// </summary>
/// <remarks>
/// 给自己界定访问范围的业务（如聊天以会话为范围，方案 H2）存取附件：上传与秒传按调用方所在的一侧存、记上来源，访问级别是「需要授权」（文件管理照旧要 <c>files.files.read</c>）；
/// 业务按自己的范围核对之后经 <see cref="SignAsync"/> 签出地址，不再要通用的文件权限。没装 Files 即没有实现
/// </remarks>
public interface IAttachmentFiles
{
    /// <summary>
    /// 上传一个附件
    /// </summary>
    /// <remarks>
    /// 大小上限、存储配额与内容去重同普通上传
    /// </remarks>
    /// <param name="context">调用上下文</param>
    /// <param name="source">来源（业务名，如 <c>chat</c>）</param>
    /// <param name="content">内容</param>
    /// <param name="fileName">原始文件名</param>
    /// <param name="contentType">内容类型</param>
    /// <returns>新文件</returns>
    Task<Result<AttachmentFile>> UploadAsync(InvocationContext context, string source, Stream content, string? fileName, string? contentType);

    /// <summary>
    /// 秒传一个附件
    /// </summary>
    /// <remarks>
    /// 同一侧已有内容相同（SHA-256 与大小）的正常文件时直接得到一条新记录，没命中为无值
    /// </remarks>
    /// <param name="context">调用上下文</param>
    /// <param name="source">来源</param>
    /// <param name="hash">SHA-256（十六进制）</param>
    /// <param name="size">大小</param>
    /// <param name="fileName">原始文件名</param>
    /// <param name="contentType">内容类型</param>
    /// <returns>新文件；没命中为无值</returns>
    Task<Result<Maybe<AttachmentFile>>> FastUploadAsync(InvocationContext context, string source, string hash, long size, string fileName, string? contentType);

    /// <summary>
    /// 按 ID 取调用方这一侧的正常文件
    /// </summary>
    /// <param name="context">调用上下文</param>
    /// <param name="fileIds">文件 ID</param>
    /// <returns>找到的文件；不在这一侧、已删除、归档或损坏的不在其中</returns>
    Task<IReadOnlyDictionary<Guid, AttachmentFile>> FindAsync(InvocationContext context, IReadOnlyCollection<Guid> fileIds);

    /// <summary>
    /// 签出一个附件的下载地址
    /// </summary>
    /// <remarks>
    /// 不再核对通用的文件权限，调用方先按自己的范围核对过；地址的有效期同 <c>files.presign</c> 的缺省值
    /// </remarks>
    /// <param name="context">调用上下文</param>
    /// <param name="fileId">文件 ID</param>
    /// <param name="download">是否以附件下载（带原始文件名）</param>
    /// <returns>地址与到期时间；文件不在这一侧或不可用为失败</returns>
    Task<Result<AttachmentLink>> SignAsync(InvocationContext context, Guid fileId, bool download);
}

/// <summary>
/// 附件文件
/// </summary>
/// <param name="Id">文件 ID</param>
/// <param name="Name">原始文件名</param>
/// <param name="ContentType">内容类型</param>
/// <param name="Size">大小（字节）</param>
/// <param name="Source">来源</param>
/// <param name="UploadedBy">上传人（用户 ID 的文本）</param>
public sealed record AttachmentFile(Guid Id, string Name, string ContentType, long Size, string Source, string? UploadedBy);

/// <summary>
/// 附件的下载地址
/// </summary>
/// <param name="Url">地址（对象存储签出，原样使用）</param>
/// <param name="ExpiresAt">到期时间（UTC）</param>
public sealed record AttachmentLink(string Url, DateTimeOffset ExpiresAt);
