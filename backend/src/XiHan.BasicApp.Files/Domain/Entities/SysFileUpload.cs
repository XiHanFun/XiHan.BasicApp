// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Files.Domain.Entities;

/// <summary>
/// 分片上传：大文件分段直传存储的一次上传
/// </summary>
/// <remarks>
/// 大文件不经应用转发：发起时在上传目标的存储里开一次分片上传（<c>IBlobMultipartStore</c>），记下配置、桶、对象键与存储给的上传标识，各分片经签名地址直传存储；这张表只是应用这边的会话记录，断点续传时已传的分片以存储列出的为准
/// 完成时不信客户端报的分片清单：按存储列出的分片核对编号齐全与总大小，拼好后回读算 SHA-256（客户端不必自己算），再照普通上传的规则落文件，与同一侧已有内容相同即共用已有的对象、刚拼好的对象删掉
/// 完成后文件的访问级别、临时与到期、标签与备注在发起时就存下，完成那一步据此落文件；<see cref="FileId"/> 发起时定下、用作对象键的最后一段，完成后指向实际得到的文件
/// 存储配额在发起与完成时各核对一次；只有发起人能续传、完成或放弃，别人的按不存在
/// 有截止时间（<c>BasicApp:Files:UploadLifetime</c>）：操作里过了 <see cref="ExpiresAt"/> 即按过期回答，作业 <c>files.uploads.expire</c> 每小时逐侧把过期的记为过期并放弃存储里没完成的分片（放弃不了只记警告）
/// 租户库实体，平台一侧租户为 <c>platform</c>；不删除，结束（完成、放弃、过期）后留作记录
/// </remarks>
[SugarTable(TableName = "sys_file_upload", TableDescription = "分片上传：大文件分段直传存储的一次上传")]
[SugarIndex("ix_sys_file_upload_tenant_id_status_expires_at", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc, nameof(ExpiresAt), OrderByType.Asc)]
public sealed class SysFileUpload : TenantScopedAuditedEntity
{
    /// <summary>
    /// 发起的用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "发起的用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 完成后得到的文件 ID
    /// </summary>
    /// <remarks>
    /// 发起时就定下，对象键的最后一段用它
    /// </remarks>
    [SugarColumn(ColumnName = "file_id", IsNullable = false, ColumnDescription = "完成后得到的文件 ID")]
    public Guid FileId { get; set; }

    /// <summary>
    /// 上传目标的存储配置 ID
    /// </summary>
    [SugarColumn(ColumnName = "storage_config_id", IsNullable = false, ColumnDescription = "上传目标的存储配置 ID")]
    public Guid StorageConfigId { get; set; }

    /// <summary>
    /// 发起时的存储类型
    /// </summary>
    [SugarColumn(ColumnName = "storage_type", IsNullable = false, ColumnDescription = "发起时的存储类型")]
    public StorageType StorageType { get; set; }

    /// <summary>
    /// 桶名
    /// </summary>
    [SugarColumn(ColumnName = "bucket", Length = 63, IsNullable = false, ColumnDescription = "桶名")]
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// 对象键
    /// </summary>
    [SugarColumn(ColumnName = "key", Length = 1024, IsNullable = false, ColumnDescription = "对象键")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 存储里的分片上传标识
    /// </summary>
    [SugarColumn(ColumnName = "upload_id", Length = 1024, IsNullable = false, ColumnDescription = "存储里的分片上传标识")]
    public string UploadId { get; set; } = string.Empty;

    /// <summary>
    /// 发起时是否允许覆盖已有对象
    /// </summary>
    /// <remarks>
    /// 完成、列分片与中止都要带同一个值（存储按它决定合成时是否条件写入）
    /// </remarks>
    [SugarColumn(ColumnName = "overwrite", IsNullable = false, ColumnDescription = "发起时是否允许覆盖已有对象")]
    public bool Overwrite { get; set; }

    /// <summary>
    /// 原始文件名
    /// </summary>
    [SugarColumn(ColumnName = "original_name", Length = 255, IsNullable = false, ColumnDescription = "原始文件名")]
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// 上传方给的内容类型
    /// </summary>
    [SugarColumn(ColumnName = "content_type", Length = 100, IsNullable = true, ColumnDescription = "上传方给的内容类型")]
    public string? ContentType { get; set; }

    /// <summary>
    /// 声明的大小（字节）
    /// </summary>
    [SugarColumn(ColumnName = "size", IsNullable = false, ColumnDescription = "声明的大小（字节）")]
    public long Size { get; set; }

    /// <summary>
    /// 每片的大小（字节）
    /// </summary>
    /// <remarks>
    /// 最后一片可以更小
    /// </remarks>
    [SugarColumn(ColumnName = "part_size", IsNullable = false, ColumnDescription = "每片的大小（字节）")]
    public long PartSize { get; set; }

    /// <summary>
    /// 分片数
    /// </summary>
    [SugarColumn(ColumnName = "part_count", IsNullable = false, ColumnDescription = "分片数")]
    public int PartCount { get; set; }

    /// <summary>
    /// 完成后文件的访问级别
    /// </summary>
    [SugarColumn(ColumnName = "access_level", IsNullable = false, ColumnDescription = "完成后文件的访问级别")]
    public FileAccessLevel AccessLevel { get; set; } = FileAccessLevel.Authorized;

    /// <summary>
    /// 完成后是否临时文件
    /// </summary>
    [SugarColumn(ColumnName = "is_temporary", IsNullable = false, ColumnDescription = "完成后是否临时文件")]
    public bool IsTemporary { get; set; }

    /// <summary>
    /// 完成后文件的到期时间
    /// </summary>
    [SugarColumn(ColumnName = "file_expires_at", IsNullable = true, ColumnDescription = "完成后文件的到期时间")]
    public DateTimeOffset? FileExpiresAt { get; set; }

    /// <summary>
    /// 完成后文件的标签（逗号分隔）
    /// </summary>
    [SugarColumn(ColumnName = "tags", Length = 500, IsNullable = true, ColumnDescription = "完成后文件的标签（逗号分隔）")]
    public string? Tags { get; set; }

    /// <summary>
    /// 完成后文件的备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "完成后文件的备注")]
    public string? Remark { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public FileUploadStatus Status { get; set; }

    /// <summary>
    /// 上传的截止时间
    /// </summary>
    [SugarColumn(ColumnName = "expires_at", IsNullable = false, ColumnDescription = "上传的截止时间")]
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 结束时间（完成、放弃、过期或失败）
    /// </summary>
    [SugarColumn(ColumnName = "ended_at", IsNullable = true, ColumnDescription = "结束时间（完成、放弃、过期或失败）")]
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>
    /// 失败的原因（错误码）
    /// </summary>
    [SugarColumn(ColumnName = "failure_code", Length = 64, IsNullable = true, ColumnDescription = "失败的原因（错误码）")]
    public string? FailureCode { get; set; }
}

/// <summary>
/// 分片上传的状态
/// </summary>
public enum FileUploadStatus
{
    /// <summary>
    /// 上传中
    /// </summary>
    Uploading = 0,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 1,

    /// <summary>
    /// 已放弃
    /// </summary>
    Aborted = 2,

    /// <summary>
    /// 已过期
    /// </summary>
    Expired = 3,

    /// <summary>
    /// 失败（拼出的内容与声明的不符）
    /// </summary>
    Failed = 4,
}
