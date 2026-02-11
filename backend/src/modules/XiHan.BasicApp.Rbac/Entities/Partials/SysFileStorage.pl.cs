#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFileStorage.pl
// Guid:9c28152c-d6e9-4396-addb-b479254bad33
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统文件存储实体（部分类 - 业务方法）
/// </summary>
public partial class SysFileStorage
{
    /// <summary>
    /// 所属文件（多对一）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(FileId))]
    public virtual SysFile? File { get; set; }

    /// <summary>
    /// 获取最优访问URL
    /// </summary>
    /// <param name="preferCdn">是否优先使用CDN</param>
    /// <returns></returns>
    public string? GetBestAccessUrl(bool preferCdn = true)
    {
        if (preferCdn && EnableCdn && !string.IsNullOrEmpty(CdnUrl))
        {
            return CdnUrl;
        }

        if (!string.IsNullOrEmpty(ExternalUrl))
        {
            return ExternalUrl;
        }

        if (!string.IsNullOrEmpty(InternalUrl))
        {
            return InternalUrl;
        }

        // 如果有自定义域名，构建URL
        if (!string.IsNullOrEmpty(CustomDomain))
        {
            return $"{CustomDomain.TrimEnd('/')}/{StoragePath.TrimStart('/')}";
        }

        return null;
    }

    /// <summary>
    /// 检查签名URL是否有效
    /// </summary>
    /// <returns></returns>
    public bool IsSignedUrlValid()
    {
        return !string.IsNullOrEmpty(SignedUrl) &&
               SignedUrlExpiresAt.HasValue &&
               SignedUrlExpiresAt.Value > DateTimeOffset.Now;
    }

    /// <summary>
    /// 标记上传成功
    /// </summary>
    public void MarkUploadSuccess()
    {
        Status = FileStorageStatus.Normal;
        UploadedAt = DateTimeOffset.Now;
        IsSynced = true;
        SyncedAt = DateTimeOffset.Now;
    }

    /// <summary>
    /// 标记上传失败
    /// </summary>
    /// <param name="reason">失败原因</param>
    public void MarkUploadFailed(string reason)
    {
        Status = FileStorageStatus.UploadFailed;
        UploadFailureReason = reason;
        RetryCount++;
    }

    /// <summary>
    /// 标记为同步中
    /// </summary>
    public void MarkSyncing()
    {
        Status = FileStorageStatus.Syncing;
        IsSynced = false;
    }

    /// <summary>
    /// 标记同步成功
    /// </summary>
    public void MarkSyncSuccess()
    {
        Status = FileStorageStatus.Normal;
        IsSynced = true;
        SyncedAt = DateTimeOffset.Now;
    }

    /// <summary>
    /// 标记同步失败
    /// </summary>
    public void MarkSyncFailed()
    {
        Status = FileStorageStatus.SyncFailed;
        IsSynced = false;
        RetryCount++;
    }

    /// <summary>
    /// 标记为已验证
    /// </summary>
    public void MarkVerified()
    {
        IsVerified = true;
        LastVerifiedAt = DateTimeOffset.Now;
        Status = FileStorageStatus.Normal;
    }

    /// <summary>
    /// 标记验证失败
    /// </summary>
    public void MarkVerificationFailed()
    {
        IsVerified = false;
        Status = FileStorageStatus.VerificationFailed;
    }

    /// <summary>
    /// 检查是否需要验证
    /// </summary>
    /// <param name="verifyIntervalHours">验证间隔（小时）</param>
    /// <returns></returns>
    public bool NeedsVerification(int verifyIntervalHours = 24)
    {
        if (!IsVerified)
        {
            return true;
        }

        if (!LastVerifiedAt.HasValue)
        {
            return true;
        }

        return DateTimeOffset.Now - LastVerifiedAt.Value > TimeSpan.FromHours(verifyIntervalHours);
    }

    /// <summary>
    /// 生成签名URL
    /// </summary>
    /// <param name="signedUrl">签名URL</param>
    /// <param name="expiresInMinutes">过期时间（分钟）</param>
    public void GenerateSignedUrl(string signedUrl, int expiresInMinutes = 60)
    {
        SignedUrl = signedUrl;
        SignedUrlExpiresAt = DateTimeOffset.Now.AddMinutes(expiresInMinutes);
    }

    /// <summary>
    /// 清除签名URL
    /// </summary>
    public void ClearSignedUrl()
    {
        SignedUrl = null;
        SignedUrlExpiresAt = null;
    }

    /// <summary>
    /// 设置为主存储
    /// </summary>
    public void SetAsPrimary()
    {
        IsPrimary = true;
        IsBackup = false;
    }

    /// <summary>
    /// 设置为备份存储
    /// </summary>
    public void SetAsBackup()
    {
        IsPrimary = false;
        IsBackup = true;
    }

    /// <summary>
    /// 检查存储是否可用
    /// </summary>
    /// <returns></returns>
    public bool IsAvailable()
    {
        return Status == FileStorageStatus.Normal && IsSynced;
    }

    /// <summary>
    /// 获取格式化的上传耗时
    /// </summary>
    /// <returns></returns>
    public string GetFormattedUploadDuration()
    {
        if (!UploadDuration.HasValue)
        {
            return "N/A";
        }

        var duration = UploadDuration.Value;

        if (duration < 1000)
        {
            return $"{duration}ms";
        }

        if (duration < 60000)
        {
            return $"{duration / 1000.0:F2}s";
        }

        return $"{duration / 60000.0:F2}min";
    }

    /// <summary>
    /// 检查是否为本地存储
    /// </summary>
    /// <returns></returns>
    public bool IsLocalStorage()
    {
        return StorageType == FileStorageType.Local;
    }

    /// <summary>
    /// 检查是否为云存储
    /// </summary>
    /// <returns></returns>
    public bool IsCloudStorage()
    {
        return StorageType is FileStorageType.AliyunOss
            or FileStorageType.TencentCos
            or FileStorageType.Minio;
    }

    /// <summary>
    /// 获取存储类型名称
    /// </summary>
    /// <returns></returns>
    public string GetStorageTypeName()
    {
        return StorageType switch
        {
            FileStorageType.Local => "本地存储",
            FileStorageType.AliyunOss => "阿里云OSS",
            FileStorageType.TencentCos => "腾讯云COS",
            FileStorageType.Minio => "MinIO",
            FileStorageType.Ftp => "FTP",
            FileStorageType.Sftp => "SFTP",
            FileStorageType.WebDav => "WebDAV",
            FileStorageType.Custom => "自定义存储",
            _ => "未知"
        };
    }

    /// <summary>
    /// 获取完整的访问路径
    /// </summary>
    /// <returns></returns>
    public string GetFullAccessPath()
    {
        if (IsLocalStorage() && !string.IsNullOrEmpty(FullPath))
        {
            return FullPath;
        }

        if (!string.IsNullOrEmpty(BucketName))
        {
            return $"{BucketName}/{StoragePath.TrimStart('/')}";
        }

        return StoragePath;
    }
}
