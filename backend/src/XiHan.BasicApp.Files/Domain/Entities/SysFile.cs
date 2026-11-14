// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Files.Domain.Entities;

/// <summary>
/// 文件：一次上传得到一条，内容落在一到多个副本（<see cref="SysFileStorage"/>）里
/// </summary>
/// <remarks>
/// 文件记录与内容的存放位置分表（<see cref="SysFileStorage"/>）：一份内容可以落在多个副本里，换主副本、校验副本不动文件本身，也不动引用它的业务；读写都取主副本
/// 落租户库（方案 5.12），平台一侧的文件 <see cref="TenantScopedAuditedEntity.TenantId"/> 为 <c>platform</c>（与日志同一做法）；头像固定存平台侧、访问级别公开，跟着用户走，任何已登录主体不分侧都能换它的签名地址
/// 内容先写本地临时文件、边写边算 SHA-256，写进存储之后才落记录，所以没有「上传中」「上传失败」这类状态
/// 同一侧 SHA-256 与大小都相同的正常文件共用存储里的对象、各有各的记录：重复上传与秒传不再多占存储，名称、访问级别、到期与标签各管各的；删到这一侧最后一条引用才删对象。存储配额却按记录算，共用对象的文件各自计入
/// 访问级别决定谁能换签名地址；签名地址指向对象本身、有效期内不再经过应用（ADR-0049），签出之后的归档、损坏或改级别都不影响它
/// 别的包经 Contracts 存取（头像、业务附件、临时文件），由业务按自己的范围先核对，不要求通用的文件权限；被 <c>SysUser.AvatarFileId</c>、<c>SysChatMessage</c> 的附件、<c>SysCodeGenHistory</c> 与 <see cref="SysExportTask"/> 按文件 ID 引用
/// 业务产出的文件（导出结果、代码生成的产物包）标为临时并带到期时间，由作业 <c>files.temporary.purge</c> 每小时逐侧分批删掉，积压的留给下一次；没标为临时的即使写了到期时间也不删；经临时文件契约读取时到期即按不存在，不等作业
/// 软删除连同副本，对象删不掉只记警告、记录照删；下载次数在独立的短事务里原子加一，不随下载期间的调用事务一直占着行
/// </remarks>
[SugarTable(TableName = "sys_file", TableDescription = "文件：一次上传得到一条，内容落在一到多个副本（SysFileStorage）里")]
[SugarIndex("ix_sys_file_tenant_id_hash_size", nameof(TenantId), OrderByType.Asc, nameof(Hash), OrderByType.Asc, nameof(Size), OrderByType.Asc)]
[SugarIndex("ix_sys_file_tenant_id_created_at", nameof(TenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
public sealed class SysFile : TenantScopedAuditedEntity
{
    /// <summary>
    /// 系统文件名（对象键的最后一段）
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 200, IsNullable = false, ColumnDescription = "系统文件名（对象键的最后一段）")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 上传时的原始文件名
    /// </summary>
    [SugarColumn(ColumnName = "original_name", Length = 255, IsNullable = false, ColumnDescription = "上传时的原始文件名")]
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// 扩展名（小写，含点）
    /// </summary>
    /// <remarks>
    /// 没有为空
    /// </remarks>
    [SugarColumn(ColumnName = "extension", Length = 20, IsNullable = true, ColumnDescription = "扩展名（小写，含点）")]
    public string? Extension { get; set; }

    /// <summary>
    /// 文件大类（按内容类型与扩展名推出）
    /// </summary>
    [SugarColumn(ColumnName = "kind", IsNullable = false, ColumnDescription = "文件大类（按内容类型与扩展名推出）")]
    public FileKind Kind { get; set; }

    /// <summary>
    /// 内容类型
    /// </summary>
    [SugarColumn(ColumnName = "content_type", Length = 100, IsNullable = false, ColumnDescription = "内容类型")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 大小（字节）
    /// </summary>
    [SugarColumn(ColumnName = "size", IsNullable = false, ColumnDescription = "大小（字节）")]
    public long Size { get; set; }

    /// <summary>
    /// 内容的 SHA-256（小写十六进制）
    /// </summary>
    [SugarColumn(ColumnName = "hash", Length = 64, IsNullable = false, ColumnDescription = "内容的 SHA-256（小写十六进制）")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public FileStatus Status { get; set; }

    /// <summary>
    /// 访问级别：谁能换取下载地址
    /// </summary>
    [SugarColumn(ColumnName = "access_level", IsNullable = false, ColumnDescription = "访问级别：谁能换取下载地址")]
    public FileAccessLevel AccessLevel { get; set; } = FileAccessLevel.Authorized;

    /// <summary>
    /// 是否临时文件（到期可清理）
    /// </summary>
    [SugarColumn(ColumnName = "is_temporary", IsNullable = false, ColumnDescription = "是否临时文件（到期可清理）")]
    public bool IsTemporary { get; set; }

    /// <summary>
    /// 到期时间
    /// </summary>
    /// <remarks>
    /// 为空不到期
    /// </remarks>
    [SugarColumn(ColumnName = "expires_at", IsNullable = true, ColumnDescription = "到期时间")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 来源：<c>upload</c>（上传）、<c>fast-upload</c>（秒传）等
    /// </summary>
    [SugarColumn(ColumnName = "source", Length = 32, IsNullable = false, ColumnDescription = "来源：upload（上传）、fast-upload（秒传）等")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 标签（逗号分隔）
    /// </summary>
    [SugarColumn(ColumnName = "tags", Length = 500, IsNullable = true, ColumnDescription = "标签（逗号分隔）")]
    public string? Tags { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }

    /// <summary>
    /// 下载次数（经下载端点读取内容的次数）
    /// </summary>
    [SugarColumn(ColumnName = "download_count", IsNullable = false, ColumnDescription = "下载次数（经下载端点读取内容的次数）")]
    public int DownloadCount { get; set; }

    /// <summary>
    /// 最近一次下载时间
    /// </summary>
    [SugarColumn(ColumnName = "last_downloaded_at", IsNullable = true, ColumnDescription = "最近一次下载时间")]
    public DateTimeOffset? LastDownloadedAt { get; set; }
}

/// <summary>
/// 文件大类
/// </summary>
public enum FileKind
{
    /// <summary>
    /// 图片
    /// </summary>
    Image = 0,

    /// <summary>
    /// 文档
    /// </summary>
    Document = 1,

    /// <summary>
    /// 视频
    /// </summary>
    Video = 2,

    /// <summary>
    /// 音频
    /// </summary>
    Audio = 3,

    /// <summary>
    /// 压缩包
    /// </summary>
    Archive = 4,

    /// <summary>
    /// 其他
    /// </summary>
    Other = 99,
}

/// <summary>
/// 文件状态
/// </summary>
/// <remarks>
/// 上传在一次请求里完成，内容写好才落记录，所以没有上传中、上传失败；删除即软删除
/// </remarks>
public enum FileStatus
{
    /// <summary>
    /// 正常
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 已归档：不能下载，可恢复
    /// </summary>
    Archived = 5,

    /// <summary>
    /// 已损坏：主副本校验不通过
    /// </summary>
    /// <remarks>
    /// 校验不通过即内容缺失或与 SHA-256 不符；切到一个校验通过的副本即恢复
    /// </remarks>
    Corrupted = 7,
}

/// <summary>
/// 文件访问级别：谁能换取下载地址（签名地址本身在有效期内不再校验身份）
/// </summary>
public enum FileAccessLevel
{
    /// <summary>
    /// 同一侧已登录即可
    /// </summary>
    Authenticated = 1,

    /// <summary>
    /// 还要持有 <c>files.files.read</c>
    /// </summary>
    Authorized = 2,

    /// <summary>
    /// 公开：任何已登录主体（不分侧）都能换签名地址
    /// </summary>
    /// <remarks>
    /// 匿名不行，签名地址照常限时
    /// 为头像这类跟着用户走的文件用；头像固定存平台侧
    /// </remarks>
    Public = 3,
}
