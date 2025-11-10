#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFile
// Guid:8c28152c-d6e9-4396-addb-b479254bad32
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 5:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统文件实体
/// </summary>
[SugarTable("sys_file", "系统文件表")]
[SugarIndex("IX_SysFile_FileHash", nameof(FileHash), OrderByType.Asc)]
[SugarIndex("IX_SysFile_FileName", nameof(FileName), OrderByType.Asc)]
[SugarIndex("IX_SysFile_FileType", nameof(FileType), OrderByType.Asc)]
[SugarIndex("IX_SysFile_TenantId", nameof(TenantId), OrderByType.Asc)]
public partial class SysFile : RbacFullAuditedEntity<RbacIdType>
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
    public virtual RbacIdType? TenantId { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    [SugarColumn(ColumnDescription = "文件名", Length = 200, IsNullable = false)]
    public virtual string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    [SugarColumn(ColumnDescription = "原始文件名", Length = 200, IsNullable = false)]
    public virtual string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [SugarColumn(ColumnDescription = "文件扩展名", Length = 20, IsNullable = true)]
    public virtual string? FileExtension { get; set; }

    /// <summary>
    /// 文件类型
    /// </summary>
    [SugarColumn(ColumnDescription = "文件类型")]
    public virtual FileType FileType { get; set; } = FileType.Other;

    /// <summary>
    /// MIME类型
    /// </summary>
    [SugarColumn(ColumnDescription = "MIME类型", Length = 100, IsNullable = true)]
    public virtual string? MimeType { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [SugarColumn(ColumnDescription = "文件大小（字节）")]
    public virtual long FileSize { get; set; } = 0;

    /// <summary>
    /// 文件哈希值
    /// </summary>
    [SugarColumn(ColumnDescription = "文件哈希值", Length = 100, IsNullable = true)]
    public virtual string? FileHash { get; set; }

    /// <summary>
    /// 存储路径
    /// </summary>
    [SugarColumn(ColumnDescription = "存储路径", Length = 500, IsNullable = false)]
    public virtual string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// 访问URL
    /// </summary>
    [SugarColumn(ColumnDescription = "访问URL", Length = 1000, IsNullable = true)]
    public virtual string? AccessUrl { get; set; }

    /// <summary>
    /// 存储类型（本地、OSS、云存储等）
    /// </summary>
    [SugarColumn(ColumnDescription = "存储类型", Length = 20, IsNullable = false)]
    public virtual string StorageType { get; set; } = "Local";

    /// <summary>
    /// 存储桶名称
    /// </summary>
    [SugarColumn(ColumnDescription = "存储桶名称", Length = 100, IsNullable = true)]
    public virtual string? BucketName { get; set; }

    /// <summary>
    /// 上传者ID
    /// </summary>
    [SugarColumn(ColumnDescription = "上传者ID", IsNullable = true)]
    public virtual long? UploaderId { get; set; }

    /// <summary>
    /// 上传IP
    /// </summary>
    [SugarColumn(ColumnDescription = "上传IP", Length = 50, IsNullable = true)]
    public virtual string? UploadIp { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    [SugarColumn(ColumnDescription = "业务类型", Length = 50, IsNullable = true)]
    public virtual string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    [SugarColumn(ColumnDescription = "业务ID", IsNullable = true)]
    public virtual long? BusinessId { get; set; }

    /// <summary>
    /// 下载次数
    /// </summary>
    [SugarColumn(ColumnDescription = "下载次数")]
    public virtual int DownloadCount { get; set; } = 0;

    /// <summary>
    /// 最后下载时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后下载时间", IsNullable = true)]
    public virtual DateTimeOffset? LastDownloadTime { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
