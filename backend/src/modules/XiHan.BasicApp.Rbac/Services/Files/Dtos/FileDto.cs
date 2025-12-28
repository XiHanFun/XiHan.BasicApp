#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileDto
// Guid:q1r2s3t4-u5v6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 17:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.Base.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Files.Dtos;

/// <summary>
/// 文件 DTO
/// </summary>
public class FileDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public XiHanBasicAppIdType? TenantId { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string? FileExtension { get; set; }

    /// <summary>
    /// 文件类型
    /// </summary>
    public FileType FileType { get; set; } = FileType.Other;

    /// <summary>
    /// MIME类型
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; } = 0;

    /// <summary>
    /// 文件哈希值
    /// </summary>
    public string? FileHash { get; set; }

    /// <summary>
    /// 存储路径
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// 访问URL
    /// </summary>
    public string? AccessUrl { get; set; }

    /// <summary>
    /// 存储类型（本地、OSS、云存储等）
    /// </summary>
    public string StorageType { get; set; } = "Local";

    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// 上传者ID
    /// </summary>
    public XiHanBasicAppIdType? UploaderId { get; set; }

    /// <summary>
    /// 上传IP
    /// </summary>
    public string? UploadIp { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    public XiHanBasicAppIdType? BusinessId { get; set; }

    /// <summary>
    /// 下载次数
    /// </summary>
    public int DownloadCount { get; set; } = 0;

    /// <summary>
    /// 最后下载时间
    /// </summary>
    public DateTimeOffset? LastDownloadTime { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 创建文件 DTO
/// </summary>
public class CreateFileDto : RbacCreationDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public XiHanBasicAppIdType? TenantId { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string? FileExtension { get; set; }

    /// <summary>
    /// 文件类型
    /// </summary>
    public FileType FileType { get; set; } = FileType.Other;

    /// <summary>
    /// MIME类型
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; } = 0;

    /// <summary>
    /// 文件哈希值
    /// </summary>
    public string? FileHash { get; set; }

    /// <summary>
    /// 存储路径
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// 访问URL
    /// </summary>
    public string? AccessUrl { get; set; }

    /// <summary>
    /// 存储类型（本地、OSS、云存储等）
    /// </summary>
    public string StorageType { get; set; } = "Local";

    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// 上传者ID
    /// </summary>
    public XiHanBasicAppIdType? UploaderId { get; set; }

    /// <summary>
    /// 上传IP
    /// </summary>
    public string? UploadIp { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    public XiHanBasicAppIdType? BusinessId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新文件 DTO
/// </summary>
public class UpdateFileDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 文件名
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// 访问URL
    /// </summary>
    public string? AccessUrl { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    public XiHanBasicAppIdType? BusinessId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

