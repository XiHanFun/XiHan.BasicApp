// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Files.Domain.Entities;

/// <summary>
/// 文件副本：文件内容在某个存储配置里的一份
/// </summary>
/// <remarks>
/// 内容落在哪从文件记录里拆出来：记下存储配置、桶、对象键与写入时的存储类型，配置之后改了类型也看得出这份当初落在哪；每个文件恰好一个主副本（<see cref="IsPrimary"/>），读写取主副本
/// 上传只写主副本；<c>files.copy-storage</c> 从主副本复制到另一条配置（每条配置至多一份），再设为主副本即把文件迁到那条配置
/// 与文件同侧，存储配置按 ID 引用本侧或平台的：租户没有自己的缺省配置时副本落在平台的配置上，照样读写
/// 共用对象就是多条副本指向同一配置、桶与对象键：删一个文件时数这一侧还有几条副本指向这个对象，没有了才从存储里删，索引按这个读法建
/// 校验按 SHA-256 重读内容，只有确认内容缺失或不符才记校验失败，存储本身不可用（网络、权限）时不改状态；主副本不通过即把文件记为损坏，换到一个正常且校验过的副本即恢复，所以主副本不能停用
/// 还有未删除副本的存储配置不能删（不分侧，平台的配置也会被租户的副本用到）；副本随文件软删除
/// </remarks>
[SugarTable(TableName = "sys_file_storage", TableDescription = "文件副本：文件内容在某个存储配置里的一份")]
[SugarIndex("ix_sys_file_storage_tenant_id_file_id", nameof(TenantId), OrderByType.Asc, nameof(FileId), OrderByType.Asc)]
[SugarIndex("ix_sys_file_storage_storage_config_id_bucket_key", nameof(StorageConfigId), OrderByType.Asc, nameof(Bucket), OrderByType.Asc, nameof(Key), OrderByType.Asc)]
public sealed class SysFileStorage : TenantScopedAuditedEntity
{
    /// <summary>
    /// 文件 ID
    /// </summary>
    [SugarColumn(ColumnName = "file_id", IsNullable = false, ColumnDescription = "文件 ID")]
    public Guid FileId { get; set; }

    /// <summary>
    /// 存储配置 ID
    /// </summary>
    [SugarColumn(ColumnName = "storage_config_id", IsNullable = false, ColumnDescription = "存储配置 ID")]
    public Guid StorageConfigId { get; set; }

    /// <summary>
    /// 写入时的存储类型（配置改了类型也能看出这份当初落在哪）
    /// </summary>
    [SugarColumn(ColumnName = "storage_type", IsNullable = false, ColumnDescription = "写入时的存储类型（配置改了类型也能看出这份当初落在哪）")]
    public StorageType StorageType { get; set; }

    /// <summary>
    /// 桶名
    /// </summary>
    [SugarColumn(ColumnName = "bucket", Length = 63, IsNullable = false, ColumnDescription = "桶名")]
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// 对象键
    /// </summary>
    /// <remarks>
    /// 格式为 <c>&lt;侧&gt;/&lt;年&gt;/&lt;月&gt;/&lt;日&gt;/&lt;文件 ID&gt;&lt;扩展名&gt;</c>
    /// </remarks>
    [SugarColumn(ColumnName = "key", Length = 1024, IsNullable = false, ColumnDescription = "对象键")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 是否主副本
    /// </summary>
    [SugarColumn(ColumnName = "is_primary", IsNullable = false, ColumnDescription = "是否主副本")]
    public bool IsPrimary { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public FileStorageStatus Status { get; set; }

    /// <summary>
    /// 最近一次校验时间
    /// </summary>
    /// <remarks>
    /// 写入时即校验过（写入的内容按 SHA-256 算过）
    /// </remarks>
    [SugarColumn(ColumnName = "verified_at", IsNullable = true, ColumnDescription = "最近一次校验时间")]
    public DateTimeOffset? VerifiedAt { get; set; }

    /// <summary>
    /// 备注（校验失败的原因等）
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注（校验失败的原因等）")]
    public string? Remark { get; set; }
}

/// <summary>
/// 副本状态
/// </summary>
public enum FileStorageStatus
{
    /// <summary>
    /// 正常
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 校验失败：内容缺失或与文件的 SHA-256 不符
    /// </summary>
    VerificationFailed = 6,

    /// <summary>
    /// 停用：不作为主副本，也不参与读取
    /// </summary>
    Unavailable = 9,
}
