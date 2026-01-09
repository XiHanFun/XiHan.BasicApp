#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFileStorage
// Guid:9c28152c-d6e9-4396-addb-b479254bad33
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 5:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统文件存储实体
/// </summary>
/// <remarks>
/// 管理文件的物理存储位置信息
/// 一个文件可以有多个存储位置（主存储、备份存储、CDN等）
/// </remarks>
[SugarTable("Sys_File_Storage", "系统文件存储表")]
[SugarIndex("IX_SysFileStorage_FileId", nameof(FileId), OrderByType.Asc)]
[SugarIndex("IX_SysFileStorage_StorageType", nameof(StorageType), OrderByType.Asc)]
[SugarIndex("IX_SysFileStorage_IsPrimary", nameof(IsPrimary), OrderByType.Desc)]
[SugarIndex("IX_SysFileStorage_FileId_StorageType", $"{nameof(FileId)},{nameof(StorageType)}", OrderByType.Asc)]
public partial class SysFileStorage : RbacFullAuditedEntity<long>
{
    #region 关联信息

    /// <summary>
    /// 文件ID（外键）
    /// </summary>
    [SugarColumn(ColumnDescription = "文件ID", IsNullable = false)]
    public virtual long FileId { get; set; }

    #endregion

    #region 存储配置

    /// <summary>
    /// 存储类型
    /// </summary>
    [SugarColumn(ColumnDescription = "存储类型")]
    public virtual Enums.StorageType StorageType { get; set; } = Enums.StorageType.Local;

    /// <summary>
    /// 存储提供商（Aliyun、Tencent、AWS、Minio等）
    /// </summary>
    [SugarColumn(ColumnDescription = "存储提供商", Length = 50, IsNullable = true)]
    public virtual string? StorageProvider { get; set; }

    /// <summary>
    /// 存储配置ID（关联存储配置表）
    /// </summary>
    [SugarColumn(ColumnDescription = "存储配置ID", IsNullable = true)]
    public virtual long? StorageConfigId { get; set; }

    /// <summary>
    /// 存储区域（地域）
    /// </summary>
    [SugarColumn(ColumnDescription = "存储区域", Length = 50, IsNullable = true)]
    public virtual string? StorageRegion { get; set; }

    #endregion

    #region 存储路径

    /// <summary>
    /// 存储桶名称（Bucket/Container）
    /// </summary>
    [SugarColumn(ColumnDescription = "存储桶名称", Length = 100, IsNullable = true)]
    public virtual string? BucketName { get; set; }

    /// <summary>
    /// 存储路径（相对路径）
    /// </summary>
    /// <remarks>
    /// 如：uploads/2025/01/10/xxx.jpg
    /// </remarks>
    [SugarColumn(ColumnDescription = "存储路径", Length = 500, IsNullable = false)]
    public virtual string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// 完整路径（绝对路径，仅本地存储使用）
    /// </summary>
    [SugarColumn(ColumnDescription = "完整路径", Length = 1000, IsNullable = true)]
    public virtual string? FullPath { get; set; }

    /// <summary>
    /// 存储目录
    /// </summary>
    [SugarColumn(ColumnDescription = "存储目录", Length = 500, IsNullable = true)]
    public virtual string? StorageDirectory { get; set; }

    #endregion

    #region 访问信息

    /// <summary>
    /// 内部访问URL（内网地址）
    /// </summary>
    [SugarColumn(ColumnDescription = "内部访问URL", Length = 1000, IsNullable = true)]
    public virtual string? InternalUrl { get; set; }

    /// <summary>
    /// 外部访问URL（公网地址）
    /// </summary>
    [SugarColumn(ColumnDescription = "外部访问URL", Length = 1000, IsNullable = true)]
    public virtual string? ExternalUrl { get; set; }

    /// <summary>
    /// CDN访问URL
    /// </summary>
    [SugarColumn(ColumnDescription = "CDN访问URL", Length = 1000, IsNullable = true)]
    public virtual string? CdnUrl { get; set; }

    /// <summary>
    /// 签名访问URL（临时访问链接）
    /// </summary>
    [SugarColumn(ColumnDescription = "签名访问URL", Length = 2000, IsNullable = true)]
    public virtual string? SignedUrl { get; set; }

    /// <summary>
    /// 签名URL过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "签名URL过期时间", IsNullable = true)]
    public virtual DateTimeOffset? SignedUrlExpiresAt { get; set; }

    /// <summary>
    /// 访问端点（Endpoint）
    /// </summary>
    [SugarColumn(ColumnDescription = "访问端点", Length = 200, IsNullable = true)]
    public virtual string? Endpoint { get; set; }

    /// <summary>
    /// 自定义域名
    /// </summary>
    [SugarColumn(ColumnDescription = "自定义域名", Length = 200, IsNullable = true)]
    public virtual string? CustomDomain { get; set; }

    #endregion

    #region 存储属性

    /// <summary>
    /// 是否为主存储
    /// </summary>
    [SugarColumn(ColumnDescription = "是否为主存储")]
    public virtual bool IsPrimary { get; set; } = true;

    /// <summary>
    /// 是否为备份存储
    /// </summary>
    [SugarColumn(ColumnDescription = "是否为备份存储")]
    public virtual bool IsBackup { get; set; } = false;

    /// <summary>
    /// 是否启用CDN加速
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用CDN加速")]
    public virtual bool EnableCdn { get; set; } = false;

    /// <summary>
    /// 是否压缩存储
    /// </summary>
    [SugarColumn(ColumnDescription = "是否压缩存储")]
    public virtual bool IsCompressed { get; set; } = false;

    /// <summary>
    /// 压缩率（百分比）
    /// </summary>
    [SugarColumn(ColumnDescription = "压缩率", IsNullable = true)]
    public virtual decimal? CompressionRatio { get; set; }

    #endregion

    #region 存储状态

    /// <summary>
    /// 存储状态
    /// </summary>
    [SugarColumn(ColumnDescription = "存储状态")]
    public virtual StorageStatus Status { get; set; } = StorageStatus.Normal;

    /// <summary>
    /// 上传时间
    /// </summary>
    [SugarColumn(ColumnDescription = "上传时间", IsNullable = true)]
    public virtual DateTimeOffset? UploadedAt { get; set; }

    /// <summary>
    /// 上传耗时（毫秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "上传耗时（毫秒）", IsNullable = true)]
    public virtual long? UploadDuration { get; set; }

    /// <summary>
    /// 上传失败原因
    /// </summary>
    [SugarColumn(ColumnDescription = "上传失败原因", Length = 500, IsNullable = true)]
    public virtual string? UploadFailureReason { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    [SugarColumn(ColumnDescription = "重试次数")]
    public virtual int RetryCount { get; set; } = 0;

    /// <summary>
    /// 最后验证时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后验证时间", IsNullable = true)]
    public virtual DateTimeOffset? LastVerifiedAt { get; set; }

    /// <summary>
    /// 验证状态（文件完整性）
    /// </summary>
    [SugarColumn(ColumnDescription = "验证状态")]
    public virtual bool IsVerified { get; set; } = false;

    #endregion

    #region 同步信息

    /// <summary>
    /// 是否已同步
    /// </summary>
    [SugarColumn(ColumnDescription = "是否已同步")]
    public virtual bool IsSynced { get; set; } = true;

    /// <summary>
    /// 同步时间
    /// </summary>
    [SugarColumn(ColumnDescription = "同步时间", IsNullable = true)]
    public virtual DateTimeOffset? SyncedAt { get; set; }

    /// <summary>
    /// 同步源存储ID
    /// </summary>
    [SugarColumn(ColumnDescription = "同步源存储ID", IsNullable = true)]
    public virtual long? SyncSourceId { get; set; }

    #endregion

    #region 访问控制

    /// <summary>
    /// 访问权限（private、public-read、public-read-write等）
    /// </summary>
    [SugarColumn(ColumnDescription = "访问权限", Length = 50, IsNullable = true)]
    public virtual string? AccessControl { get; set; }

    /// <summary>
    /// 存储类别（标准、低频、归档等）
    /// </summary>
    [SugarColumn(ColumnDescription = "存储类别", Length = 50, IsNullable = true)]
    public virtual string? StorageClass { get; set; }

    /// <summary>
    /// 缓存控制
    /// </summary>
    [SugarColumn(ColumnDescription = "缓存控制", Length = 100, IsNullable = true)]
    public virtual string? CacheControl { get; set; }

    #endregion

    #region 成本统计

    /// <summary>
    /// 实际占用空间（字节）
    /// </summary>
    [SugarColumn(ColumnDescription = "实际占用空间（字节）", IsNullable = true)]
    public virtual long? ActualSize { get; set; }

    /// <summary>
    /// 存储成本（元/月）
    /// </summary>
    [SugarColumn(ColumnDescription = "存储成本（元/月）", IsNullable = true)]
    public virtual decimal? StorageCost { get; set; }

    /// <summary>
    /// 流量成本（元）
    /// </summary>
    [SugarColumn(ColumnDescription = "流量成本（元）", IsNullable = true)]
    public virtual decimal? TrafficCost { get; set; }

    #endregion

    #region 其他

    /// <summary>
    /// 排序号（用于多存储优先级）
    /// </summary>
    [SugarColumn(ColumnDescription = "排序号")]
    public virtual int SortOrder { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }

    /// <summary>
    /// 扩展数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展数据", Length = 2000, IsNullable = true, ColumnDataType = "json")]
    public virtual string? ExtensionData { get; set; }

    #endregion

    #region 导航属性

    /// <summary>
    /// 所属文件（多对一）
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(FileId))]
    public virtual SysFile? File { get; set; }

    #endregion
}
