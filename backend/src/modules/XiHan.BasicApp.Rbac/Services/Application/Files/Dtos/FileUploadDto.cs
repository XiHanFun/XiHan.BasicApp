#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileUploadDto
// Guid:d6e7f8a9-b0c1-4f2d-e3f4-a5b6c7d8e9f0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/10 10:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Services.Application.Files.Dtos;

/// <summary>
/// 文件上传对象
/// </summary>
public class FileUploadDto
{
    /// <summary>
    /// 文件流
    /// </summary>
    public Stream FileStream { get; set; } = null!;

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = null!;

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 内容类型
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 上传IP
    /// </summary>
    public string? UploadIp { get; set; }

    /// <summary>
    /// 上传来源
    /// </summary>
    public string? UploadSource { get; set; }

    /// <summary>
    /// 是否公开
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// 是否临时文件
    /// </summary>
    public bool IsTemporary { get; set; } = false;

    /// <summary>
    /// 标签
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// 是否启用CDN
    /// </summary>
    public bool EnableCdn { get; set; } = false;

    /// <summary>
    /// 是否启用去重
    /// </summary>
    public bool EnableDeduplication { get; set; } = true;

    /// <summary>
    /// 是否启用分片上传
    /// </summary>
    public bool EnableChunkedUpload { get; set; } = true;

    /// <summary>
    /// 分片阈值（字节，超过此大小使用分片上传）
    /// </summary>
    public int ChunkThreshold { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// 分片大小
    /// </summary>
    public int ChunkSize { get; set; } = 5 * 1024 * 1024; // 5MB

    /// <summary>
    /// 是否生成缩略图（图片）
    /// </summary>
    public bool GenerateThumbnail { get; set; } = false;

    /// <summary>
    /// 是否提取视频封面
    /// </summary>
    public bool ExtractVideoCover { get; set; } = false;

    /// <summary>
    /// 进度回调
    /// </summary>
    public Action<long, long>? ProgressCallback { get; set; }
}
