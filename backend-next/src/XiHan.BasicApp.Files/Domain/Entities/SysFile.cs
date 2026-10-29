// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Files.Domain.Entities;

/// <summary>
/// 文件：一次上传得到一条，内容落在一到多个副本（<see cref="SysFileStorage"/>）里。
/// </summary>
/// <remarks>
/// 租户库实体，方案 5.12。读写取主副本。
/// 平台一侧的文件 <see cref="TenantScopedAuditedEntity.TenantId"/> 为 <c>platform</c>（与日志同一做法）。
/// 同一侧内容相同（SHA-256 相同）的文件共用对象，各有各的记录，删到最后一条才删对象。
/// </remarks>
public sealed class SysFile : TenantScopedAuditedEntity
{
    /// <summary>
    /// 系统文件名（对象键的最后一段）。
    /// </summary>
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 上传时的原始文件名。
    /// </summary>
    [StringLength(255)]
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// 扩展名（小写，含点）。
    /// </summary>
    /// <remarks>
    /// 没有为空。
    /// </remarks>
    [StringLength(20)]
    public string? Extension { get; set; }

    /// <summary>
    /// 文件大类（按内容类型与扩展名推出）。
    /// </summary>
    public FileKind Kind { get; set; }

    /// <summary>
    /// 内容类型。
    /// </summary>
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 大小（字节）。
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 内容的 SHA-256（小写十六进制）。
    /// </summary>
    [StringLength(64)]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 状态。
    /// </summary>
    public FileStatus Status { get; set; }

    /// <summary>
    /// 访问级别：谁能换取下载地址。
    /// </summary>
    public FileAccessLevel AccessLevel { get; set; } = FileAccessLevel.Authorized;

    /// <summary>
    /// 是否临时文件（到期可清理）。
    /// </summary>
    public bool IsTemporary { get; set; }

    /// <summary>
    /// 到期时间。
    /// </summary>
    /// <remarks>
    /// 为空不到期。
    /// </remarks>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 来源：<c>upload</c>（上传）、<c>fast-upload</c>（秒传）等。
    /// </summary>
    [StringLength(32)]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 标签（逗号分隔）。
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }

    /// <summary>
    /// 下载次数（经下载端点读取内容的次数）。
    /// </summary>
    public int DownloadCount { get; set; }

    /// <summary>
    /// 最近一次下载时间。
    /// </summary>
    public DateTimeOffset? LastDownloadedAt { get; set; }
}

/// <summary>
/// 文件大类。
/// </summary>
/// <remarks>
/// 4.x 同。
/// </remarks>
public enum FileKind
{
    /// <summary>
    /// 图片。
    /// </summary>
    Image = 0,

    /// <summary>
    /// 文档。
    /// </summary>
    Document = 1,

    /// <summary>
    /// 视频。
    /// </summary>
    Video = 2,

    /// <summary>
    /// 音频。
    /// </summary>
    Audio = 3,

    /// <summary>
    /// 压缩包。
    /// </summary>
    Archive = 4,

    /// <summary>
    /// 其他。
    /// </summary>
    Other = 99,
}

/// <summary>
/// 文件状态。
/// </summary>
/// <remarks>
/// 上传在一次请求里完成，内容写好才落记录，所以没有 4.x 的上传中、上传失败；删除即软删除。
/// </remarks>
public enum FileStatus
{
    /// <summary>
    /// 正常。
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 已归档：不能下载，可恢复。
    /// </summary>
    Archived = 5,

    /// <summary>
    /// 已损坏：主副本校验不通过。
    /// </summary>
    /// <remarks>
    /// 校验不通过即内容缺失或与 SHA-256 不符；切到一个校验通过的副本即恢复。
    /// </remarks>
    Corrupted = 7,
}

/// <summary>
/// 文件访问级别：谁能换取下载地址（签名地址本身在有效期内不再校验身份）。
/// </summary>
public enum FileAccessLevel
{
    /// <summary>
    /// 同一侧已登录即可。
    /// </summary>
    Authenticated = 1,

    /// <summary>
    /// 还要持有 <c>files.files.read</c>。
    /// </summary>
    Authorized = 2,

    /// <summary>
    /// 公开：任何已登录主体（不分侧）都能换签名地址。
    /// </summary>
    /// <remarks>
    /// 匿名不行，签名地址照常限时。
    /// 4.x 同为头像这类跟着用户走的文件用；头像固定存平台侧。
    /// </remarks>
    Public = 3,
}
