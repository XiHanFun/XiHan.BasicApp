#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFileDto
// Guid:a9b0c1d2-e3f4-5678-9012-345a67890123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统文件创建 DTO
/// </summary>
public class SysFileCreateDto : RbacCreationDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

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
    /// 存储类型
    /// </summary>
    public string StorageType { get; set; } = "Local";

    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// 上传者ID
    /// </summary>
    public long? UploaderId { get; set; }

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
    public long? BusinessId { get; set; }

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
/// 系统文件更新 DTO
/// </summary>
public class SysFileUpdateDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 访问URL
    /// </summary>
    public string? AccessUrl { get; set; }

    /// <summary>
    /// 下载次数
    /// </summary>
    public int DownloadCount { get; set; }

    /// <summary>
    /// 最后下载时间
    /// </summary>
    public DateTimeOffset? LastDownloadTime { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统文件查询 DTO
/// </summary>
public class SysFileGetDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

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
    /// 存储类型
    /// </summary>
    public string StorageType { get; set; } = "Local";

    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// 上传者ID
    /// </summary>
    public long? UploaderId { get; set; }

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
    public long? BusinessId { get; set; }

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
