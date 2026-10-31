// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Files.Domain.Entities;

/// <summary>
/// 文件副本：文件内容在某个存储配置里的一份。
/// </summary>
/// <remarks>
/// 租户库实体，记下配置、桶与对象键。每个文件恰好一个主副本，读写取主副本。
/// </remarks>
[SugarIndex("ix_{table}_tenant_id_file_id", nameof(TenantId), OrderByType.Asc, nameof(FileId), OrderByType.Asc)]
[SugarIndex("ix_{table}_storage_config_id_bucket_key", nameof(StorageConfigId), OrderByType.Asc, nameof(Bucket), OrderByType.Asc, nameof(Key), OrderByType.Asc)]
public sealed class SysFileStorage : TenantScopedAuditedEntity
{
    /// <summary>
    /// 文件 ID。
    /// </summary>
    public Guid FileId { get; set; }

    /// <summary>
    /// 存储配置 ID。
    /// </summary>
    public Guid StorageConfigId { get; set; }

    /// <summary>
    /// 写入时的存储类型（配置改了类型也能看出这份当初落在哪）。
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
    /// <remarks>
    /// 格式为 <c>&lt;侧&gt;/&lt;年&gt;/&lt;月&gt;/&lt;日&gt;/&lt;文件 ID&gt;&lt;扩展名&gt;</c>。
    /// </remarks>
    [StringLength(1024)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 是否主副本。
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public FileStorageStatus Status { get; set; }

    /// <summary>
    /// 最近一次校验时间。
    /// </summary>
    /// <remarks>
    /// 写入时即校验过（写入的内容按 SHA-256 算过）。
    /// </remarks>
    public DateTimeOffset? VerifiedAt { get; set; }

    /// <summary>
    /// 备注（校验失败的原因等）。
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 副本状态。
/// </summary>
public enum FileStorageStatus
{
    /// <summary>
    /// 正常。
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 校验失败：内容缺失或与文件的 SHA-256 不符。
    /// </summary>
    VerificationFailed = 6,

    /// <summary>
    /// 停用：不作为主副本，也不参与读取。
    /// </summary>
    Unavailable = 9,
}
