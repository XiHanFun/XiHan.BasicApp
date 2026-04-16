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
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统文件实体
/// 文件元数据聚合根：承载文件身份、业务关联、统计信息；物理存储位置由 SysFileStorage 记录
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表只存"逻辑文件"（Hash/原始名/业务归属等）；"实际存在哪几个位置"由 SysFileStorage 一对多承载
///
/// 关联：
/// - 反向：SysFileStorage.FileId（一文件多副本：主存储/备份/CDN）
///
/// 写入：
/// - TenantId + FileHash 组合用于租户内去重（IX_TeId_FiHa），上传前先查同 hash 实现"秒传"
/// - IsTemporary=true 的文件应设置 ExpiresAt，后台定时清理
/// - Status 变更需同步更新统计（引用计数等）
///
/// 查询：
/// - 按业务关联查文件：BusinessType/BusinessId 过滤
/// - 按类型/状态分页：IX_FiTy / IX_TeId_St
/// - 过期临时文件扫描：IX_ExAt
///
/// 删除：
/// - 仅软删；物理删除由独立清理任务按策略批量执行（同时删除 SysFileStorage 物理文件）
///
/// 状态：
/// - Status: 正常/已删除/已损坏/已过期等
///
/// 场景：
/// - 文件上传（秒传、断点续传）
/// - 业务附件绑定（订单附件、审批附件）
/// - 存量文件管理与清理
/// </remarks>
[SugarTable("SysFile", "系统文件表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_FiNa", nameof(FileName), OrderByType.Asc)]
[SugarIndex("IX_{table}_FiTy", nameof(FileType), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsTe", nameof(IsTemporary), OrderByType.Asc)]
[SugarIndex("IX_{table}_ExAt", nameof(ExpiresAt), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_FiHa", nameof(TenantId), OrderByType.Asc, nameof(FileHash), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysFile : BasicAppAggregateRoot
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
    public virtual string? ExtendData { get; set; }

    #endregion
}
