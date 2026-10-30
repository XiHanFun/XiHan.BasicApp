// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Files.Domain.Entities;

/// <summary>
/// 分片上传：大文件分段直传存储的一次上传。
/// </summary>
/// <remarks>
/// 租户库实体，平台一侧租户为 <c>platform</c>；4.x 没有分片上传。
/// 发起时选好上传目标配置、对象键并在存储里开一次分片上传（<c>IBlobMultipartStore</c>），各分片经签名地址直传存储；
/// 完成时由应用按存储列出的分片拼成对象，回读算 SHA-256，再落文件记录（内容与同一侧已有的相同即共用已有的对象）。
/// 只有发起人能续传、完成或放弃；过了 <see cref="ExpiresAt"/> 记为过期并放弃存储里的分片。
/// </remarks>
public sealed class SysFileUpload : TenantScopedAuditedEntity
{
    /// <summary>
    /// 发起的用户。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 完成后得到的文件 ID。
    /// </summary>
    /// <remarks>
    /// 发起时就定下，对象键的最后一段用它。
    /// </remarks>
    public Guid FileId { get; set; }

    /// <summary>
    /// 上传目标的存储配置 ID。
    /// </summary>
    public Guid StorageConfigId { get; set; }

    /// <summary>
    /// 发起时的存储类型。
    /// </summary>
    public StorageType StorageType { get; set; }

    /// <summary>
    /// 桶名。
    /// </summary>
    [StringLength(63)]
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// 对象键。
    /// </summary>
    [StringLength(1024)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 存储里的分片上传标识。
    /// </summary>
    [StringLength(1024)]
    public string UploadId { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名。
    /// </summary>
    [StringLength(255)]
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// 上传方给的内容类型。
    /// </summary>
    [StringLength(100)]
    public string? ContentType { get; set; }

    /// <summary>
    /// 声明的大小（字节）。
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 每片的大小（字节）。
    /// </summary>
    /// <remarks>
    /// 最后一片可以更小。
    /// </remarks>
    public long PartSize { get; set; }

    /// <summary>
    /// 分片数。
    /// </summary>
    public int PartCount { get; set; }

    /// <summary>
    /// 完成后文件的访问级别。
    /// </summary>
    public FileAccessLevel AccessLevel { get; set; } = FileAccessLevel.Authorized;

    /// <summary>
    /// 完成后是否临时文件。
    /// </summary>
    public bool IsTemporary { get; set; }

    /// <summary>
    /// 完成后文件的到期时间。
    /// </summary>
    public DateTimeOffset? FileExpiresAt { get; set; }

    /// <summary>
    /// 完成后文件的标签（逗号分隔）。
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 完成后文件的备注。
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public FileUploadStatus Status { get; set; }

    /// <summary>
    /// 上传的截止时间。
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 结束时间（完成、放弃、过期或失败）。
    /// </summary>
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>
    /// 失败的原因（错误码）。
    /// </summary>
    [StringLength(64)]
    public string? FailureCode { get; set; }
}

/// <summary>
/// 分片上传的状态。
/// </summary>
public enum FileUploadStatus
{
    /// <summary>
    /// 上传中。
    /// </summary>
    Uploading = 0,

    /// <summary>
    /// 已完成。
    /// </summary>
    Completed = 1,

    /// <summary>
    /// 已放弃。
    /// </summary>
    Aborted = 2,

    /// <summary>
    /// 已过期。
    /// </summary>
    Expired = 3,

    /// <summary>
    /// 失败（拼出的内容与声明的不符）。
    /// </summary>
    Failed = 4,
}
