#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFile
// Guid:8c28152c-d6e9-4396-addb-b479254bad32
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统文件实体
/// </summary>
/// <remarks>
/// 文件元数据的聚合根，负责管理文件的基本信息、业务关联和统计信息
/// 一个文件可以对应多个存储位置（主存储、备份、CDN等）
/// </remarks>
[SugarTable("Sys_File", "系统文件表")]
[SugarIndex("IX_SysFile_FileHash", nameof(FileHash), OrderByType.Asc)]
[SugarIndex("IX_SysFile_FileName", nameof(FileName), OrderByType.Asc)]
[SugarIndex("IX_SysFile_FileType", nameof(FileType), OrderByType.Asc)]
[SugarIndex("IX_SysFile_TenantId", nameof(TenantId), OrderByType.Asc)]
[SugarIndex("IX_SysFile_Status", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysFile_CreatedTime", nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_SysFile_IsTemporary", nameof(IsTemporary), OrderByType.Asc)]
[SugarIndex("IX_SysFile_ExpiresAt", nameof(ExpiresAt), OrderByType.Asc)]
public partial class SysFile : RbacAggregateRoot<long>
{
    #region 基本信息

    /// <summary>
    /// 文件名（系统生成的唯一文件名）
    /// </summary>
    [SugarColumn(ColumnDescription = "文件名", Length = 200, IsNullable = false)]
    public virtual string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名（用户上传时的文件名）
    /// </summary>
    [SugarColumn(ColumnDescription = "原始文件名", Length = 200, IsNullable = false)]
    public virtual string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名（含点，如：.jpg）
    /// </summary>
    [SugarColumn(ColumnDescription = "文件扩展名", Length = 20, IsNullable = true)]
    public virtual string? FileExtension { get; set; }

    /// <summary>
    /// 文件类型
    /// </summary>
    [SugarColumn(ColumnDescription = "文件类型")]
    public virtual FileType FileType { get; set; } = FileType.Other;

    /// <summary>
    /// MIME类型（如：image/jpeg）
    /// </summary>
    [SugarColumn(ColumnDescription = "MIME类型", Length = 100, IsNullable = true)]
    public virtual string? MimeType { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [SugarColumn(ColumnDescription = "文件大小（字节）")]
    public virtual long FileSize { get; set; } = 0;

    /// <summary>
    /// 文件哈希值（用于去重和完整性校验）
    /// </summary>
    /// <remarks>
    /// 建议使用 SHA256 或 MD5
    /// </remarks>
    [SugarColumn(ColumnDescription = "文件哈希值", Length = 100, IsNullable = true)]
    public virtual string? FileHash { get; set; }

    /// <summary>
    /// 哈希算法类型
    /// </summary>
    [SugarColumn(ColumnDescription = "哈希算法类型", Length = 20, IsNullable = true)]
    public virtual string? HashAlgorithm { get; set; }

    #endregion

    #region 图片/视频特有信息

    /// <summary>
    /// 图片/视频宽度
    /// </summary>
    [SugarColumn(ColumnDescription = "宽度", IsNullable = true)]
    public virtual int? Width { get; set; }

    /// <summary>
    /// 图片/视频高度
    /// </summary>
    [SugarColumn(ColumnDescription = "高度", IsNullable = true)]
    public virtual int? Height { get; set; }

    /// <summary>
    /// 视频/音频时长（秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "时长（秒）", IsNullable = true)]
    public virtual int? Duration { get; set; }

    /// <summary>
    /// 缩略图文件ID
    /// </summary>
    [SugarColumn(ColumnDescription = "缩略图文件ID", IsNullable = true)]
    public virtual long? ThumbnailFileId { get; set; }

    #endregion

    #region 上传信息

    /// <summary>
    /// 上传IP
    /// </summary>
    [SugarColumn(ColumnDescription = "上传IP", Length = 50, IsNullable = true)]
    public virtual string? UploadIp { get; set; }

    /// <summary>
    /// 上传来源（Web、App、API等）
    /// </summary>
    [SugarColumn(ColumnDescription = "上传来源", Length = 50, IsNullable = true)]
    public virtual string? UploadSource { get; set; }

    #endregion

    #region 访问统计

    /// <summary>
    /// 下载次数
    /// </summary>
    [SugarColumn(ColumnDescription = "下载次数")]
    public virtual int DownloadCount { get; set; } = 0;

    /// <summary>
    /// 访问次数
    /// </summary>
    [SugarColumn(ColumnDescription = "访问次数")]
    public virtual int ViewCount { get; set; } = 0;

    /// <summary>
    /// 最后下载时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后下载时间", IsNullable = true)]
    public virtual DateTimeOffset? LastDownloadTime { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后访问时间", IsNullable = true)]
    public virtual DateTimeOffset? LastAccessTime { get; set; }

    #endregion

    #region 安全与权限

    /// <summary>
    /// 是否公开访问
    /// </summary>
    [SugarColumn(ColumnDescription = "是否公开访问")]
    public virtual bool IsPublic { get; set; } = true;

    /// <summary>
    /// 是否需要授权访问
    /// </summary>
    [SugarColumn(ColumnDescription = "是否需要授权访问")]
    public virtual bool RequireAuth { get; set; } = false;

    /// <summary>
    /// 访问权限（角色、用户等）
    /// </summary>
    [SugarColumn(ColumnDescription = "访问权限", Length = 500, IsNullable = true)]
    public virtual string? AccessPermissions { get; set; }

    /// <summary>
    /// 是否加密存储
    /// </summary>
    [SugarColumn(ColumnDescription = "是否加密存储")]
    public virtual bool IsEncrypted { get; set; } = false;

    /// <summary>
    /// 加密密钥ID
    /// </summary>
    [SugarColumn(ColumnDescription = "加密密钥ID", Length = 100, IsNullable = true)]
    public virtual string? EncryptionKeyId { get; set; }

    #endregion

    #region 生命周期管理

    /// <summary>
    /// 过期时间（用于临时文件）
    /// </summary>
    [SugarColumn(ColumnDescription = "过期时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 是否为临时文件
    /// </summary>
    [SugarColumn(ColumnDescription = "是否为临时文件")]
    public virtual bool IsTemporary { get; set; } = false;

    /// <summary>
    /// 保留天数（0表示永久保留）
    /// </summary>
    [SugarColumn(ColumnDescription = "保留天数")]
    public virtual int RetentionDays { get; set; } = 0;

    #endregion

    #region 状态与其他

    /// <summary>
    /// 文件状态
    /// </summary>
    [SugarColumn(ColumnDescription = "文件状态")]
    public virtual FileStatus Status { get; set; } = FileStatus.Normal;

    /// <summary>
    /// 标签（用于分类和搜索）
    /// </summary>
    [SugarColumn(ColumnDescription = "标签", Length = 500, IsNullable = true)]
    public virtual string? Tags { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }

    /// <summary>
    /// 扩展数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展数据", Length = 2000, IsNullable = true)]
    public virtual string? ExtensionData { get; set; }

    #endregion
}
