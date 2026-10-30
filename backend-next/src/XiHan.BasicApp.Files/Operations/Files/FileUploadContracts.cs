// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;

namespace XiHan.BasicApp.Files.Operations.Files;

/// <summary>
/// <c>files.start-upload</c> 的请求。
/// </summary>
/// <param name="OriginalName">原始文件名。</param>
/// <param name="Size">大小（字节）。</param>
/// <param name="ContentType">内容类型。</param>
/// <param name="AccessLevel">完成后文件的访问级别。</param>
/// <param name="IsTemporary">完成后是否临时文件。</param>
/// <param name="ExpiresAt">完成后文件的到期时间。</param>
/// <param name="Tags">完成后文件的标签（逗号分隔）。</param>
/// <param name="Remark">完成后文件的备注。</param>
public sealed record StartUploadRequest(
    [Required, StringLength(255, MinimumLength = 1)] string OriginalName,
    [Range(1, long.MaxValue)] long Size,
    [StringLength(100)] string? ContentType = null,
    FileAccessLevel AccessLevel = FileAccessLevel.Authorized,
    bool IsTemporary = false,
    DateTimeOffset? ExpiresAt = null,
    [StringLength(500)] string? Tags = null,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成发起的内容。
    /// </summary>
    /// <returns>发起的内容。</returns>
    public FileUploadDefinition ToDefinition() => new(OriginalName, Size, ContentType, new FileDetails(AccessLevel, IsTemporary, ExpiresAt, Tags, Remark));
}

/// <summary>
/// 按 ID 指定一次分片上传。
/// </summary>
/// <param name="UploadId">分片上传 ID。</param>
public sealed record UploadSessionRequest([Required] Guid UploadId);

/// <summary>
/// <c>files.sign-upload-parts</c> 的请求。
/// </summary>
/// <param name="UploadId">分片上传 ID。</param>
/// <param name="PartNumbers">要签地址的分片编号（1 到分片数，一次至多 100 个）。</param>
public sealed record SignUploadPartsRequest([Required] Guid UploadId, [Required, MinLength(1), MaxLength(FileUploadService.MaxSignedParts)] IReadOnlyList<int> PartNumbers);

/// <summary>
/// 分片上传。
/// </summary>
/// <param name="Id">ID。</param>
/// <param name="FileId">得到的文件 ID（完成之前是将来的文件 ID；内容与已有的相同时完成后指向共用对象的新记录）。</param>
/// <param name="OriginalName">原始文件名。</param>
/// <param name="Size">大小（字节）。</param>
/// <param name="PartSize">每片的大小（字节，最后一片可以更小）。</param>
/// <param name="PartCount">分片数。</param>
/// <param name="Status">状态（上传中但已过时限的为过期）。</param>
/// <param name="ExpiresAt">上传的截止时间。</param>
/// <param name="EndedAt">结束时间。</param>
/// <param name="UploadedParts">已上传的分片编号（上传中才有）。</param>
public sealed record FileUploadResponse(
    Guid Id,
    Guid FileId,
    string OriginalName,
    long Size,
    long PartSize,
    int PartCount,
    FileUploadStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? EndedAt,
    IReadOnlyList<int> UploadedParts)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    /// <param name="upload">分片上传。</param>
    /// <param name="status">当前的状态。</param>
    /// <param name="uploadedParts">已上传的分片编号。</param>
    /// <returns>响应。</returns>
    public static FileUploadResponse From(SysFileUpload upload, FileUploadStatus status, IReadOnlyList<int> uploadedParts)
    {
        ArgumentNullException.ThrowIfNull(upload);
        return new FileUploadResponse(upload.Id, upload.FileId, upload.OriginalName, upload.Size, upload.PartSize, upload.PartCount, status, upload.ExpiresAt, upload.EndedAt, uploadedParts);
    }
}

/// <summary>
/// 一个分片的上传地址。
/// </summary>
/// <remarks>
/// 不带凭据把这一片 PUT 到地址；本地存储是相对接口根的路径（<c>/blobs/…</c>），S3 兼容存储是直连存储的完整地址。
/// </remarks>
/// <param name="PartNumber">分片编号。</param>
/// <param name="Url">地址。</param>
/// <param name="ExpiresAt">到期时间。</param>
public sealed record SignedPartResponse(int PartNumber, string Url, DateTimeOffset ExpiresAt);

/// <summary>
/// <c>files.sign-upload-parts</c> 的响应。
/// </summary>
/// <param name="Parts">分片的上传地址，按编号升序。</param>
public sealed record SignUploadPartsResponse(IReadOnlyList<SignedPartResponse> Parts);
